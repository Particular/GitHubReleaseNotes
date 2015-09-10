using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace ReleaseNotesCompiler
{
    public class ReleaseNotesBuilder
    {
        IGitHubClient gitHubClient;
        string user;
        string repository;
        string milestoneTitle;
        List<Milestone> milestones;
        Milestone targetMilestone;

        public ReleaseNotesBuilder(IGitHubClient gitHubClient, string user, string repository, string milestoneTitle)
        {
            this.gitHubClient = gitHubClient;
            this.user = user;
            this.repository = repository;
            this.milestoneTitle = milestoneTitle;
        }

        public static string LabelPrefix
        {
            get { return "Type: "; }
        }

        public async Task<string> BuildReleaseNotes()
        {
            LoadMilestones();

            GetTargetMilestone();
            var issues = await GetIssues(targetMilestone);
            var stringBuilder = new StringBuilder();
            var previousMilestone = GetPreviousMilestone();
            var numberOfCommits = await gitHubClient.GetNumberOfCommitsBetween(previousMilestone, targetMilestone);

            if (issues.Count > 0)
            {
                var issuesText = String.Format(issues.Count == 1 ? "{0} issue" : "{0} issues", issues.Count);

                if (numberOfCommits > 0)
                {
                    var commitsLink = GetCommitsLink(previousMilestone);
                    var commitsText = String.Format(numberOfCommits == 1 ? "{0} commit" : "{0} commits", numberOfCommits);

                    stringBuilder.AppendFormat(@"As part of this release we had [{0}]({1}) which resulted in [{2}]({3}) being closed.", commitsText, commitsLink, issuesText, targetMilestone.HtmlUrl());
                }
                else
                {
                    stringBuilder.AppendFormat(@"As part of this release we had [{0}]({1}) closed.", issuesText, targetMilestone.HtmlUrl());
                }
            }
            else if (numberOfCommits > 0)
            {
                var commitsLink = GetCommitsLink(previousMilestone);
                var commitsText = String.Format(numberOfCommits == 1 ? "{0} commit" : "{0} commits", numberOfCommits);
                stringBuilder.AppendFormat(@"As part of this release we had [{0}]({1}).", commitsText, commitsLink);
            }
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(targetMilestone.Description);
            stringBuilder.AppendLine();

            AddIssues(stringBuilder, issues);

            await AddFooter(stringBuilder);

            return stringBuilder.ToString();
        }

        Milestone GetPreviousMilestone()
        {
            var currentVersion = targetMilestone.Version();
            return milestones
                .OrderByDescending(m => m.Version())
                .Distinct().ToList()
                .SkipWhile(x => x.Version() >= currentVersion)
                .FirstOrDefault();
        }

        string GetCommitsLink(Milestone previousMilestone)
        {
            if (previousMilestone == null)
            {
                return string.Format("https://github.com/{0}/{1}/commits/{2}", user, repository, targetMilestone.Title);
            }
            return string.Format("https://github.com/{0}/{1}/compare/{2}...{3}", user, repository, previousMilestone.Title, targetMilestone.Title);
        }

        void AddIssues(StringBuilder stringBuilder, List<Issue> issues)
        {
            Append(issues, "Feature", stringBuilder);
            Append(issues, "Bug", stringBuilder);
        }

        static async Task AddFooter(StringBuilder stringBuilder)
        {
            var file = new FileInfo("footer.md");

            if (!file.Exists)
            {
                file = new FileInfo("footer.txt");
            }

            if (!file.Exists)
            {
                stringBuilder.Append(@"## Where to get it
You can download this release from [nuget](https://www.nuget.org/profiles/nservicebus/)");
                return;
            }

            using (var reader = file.OpenText())
            {
                stringBuilder.Append(await reader.ReadToEndAsync());
            }
        }

        void LoadMilestones()
        {
            milestones = gitHubClient.GetMilestones();
        }

        async Task<List<Issue>> GetIssues(Milestone milestone)
        {
            var issues = await gitHubClient.GetIssues(milestone);
            foreach (var issue in issues)
            {
                CheckForValidLabels(issue);
            }
            return issues;
        }

        static void CheckForValidLabels(Issue issue)
        {
            if (issue.Labels.Count(label => label.Name.StartsWith(LabelPrefix)) != 1)
            {
                var message = string.Format(
                    CultureInfo.CurrentCulture,
                    "Bad issue {0}. Expected to find a single label starting with '{1}'.",
                    issue.HtmlUrl,
                    LabelPrefix);

                throw new InvalidOperationException(message);
            }
        }

        void Append(IEnumerable<Issue> issues, string labelName, StringBuilder builder)
        {
            var relevantIssues = issues
                .Where(issue => issue.Labels.Any(label => label.Name == LabelPrefix + labelName))
                .ToList();

            if (relevantIssues.Any())
            {
                builder.AppendFormat(relevantIssues.Count == 1 ? "__{0}__\r\n" : "__{0}s__\r\n", labelName);

                foreach (var issue in relevantIssues)
                {
                    builder.AppendFormat("- [__#{0}__]({1}) {2}\r\n", issue.Number, issue.HtmlUrl, issue.Title);
                }

                builder.AppendLine();
            }
        }

        void GetTargetMilestone()
        {
            targetMilestone = milestones.FirstOrDefault(x => x.Title == milestoneTitle);
            if (targetMilestone == null)
            {
                throw new Exception(string.Format("Could not find milestone for '{0}'.", milestoneTitle));
            }
        }
    }
}
