using System;
using System.Collections.Generic;
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
        IReadOnlyList<Milestone> milestones;
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
            milestones = await gitHubClient.GetMilestones();

            GetTargetMilestone();
            var issues = await gitHubClient.GetIssues(targetMilestone);
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

        void AddIssues(StringBuilder builder, List<Issue> issues)
        {
            var bugs = issues
               .Where(issue => issue.Labels.Any(label => label.Name == "Type: Bug"))
               .ToList();

            if (bugs.Any())
            {
                PrintHeading("Bugs", builder);

                PrintIssue(builder, bugs);

                builder.AppendLine();
            }

            var others = issues.Where(issue =>!issue.Labels.Any() || issue.Labels.Any(label => label.Name != "Type: Refactoring" && label.Name != "Type: Bug"))
                         .ToList();

            if (others.Any())
            {
                PrintHeading("Improvements/Features", builder);

                PrintIssue(builder, others);

                builder.AppendLine();
            }
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



        static void PrintHeading(string labelName, StringBuilder builder)
        {
            builder.AppendFormat($"__{labelName}__\r\n");
        }

        static void PrintIssue(StringBuilder builder, List<Issue> relevantIssues)
        {
            foreach (var issue in relevantIssues)
            {
                builder.Append($"- [__#{issue.Number}__]({issue.HtmlUrl}) {issue.Title}\r\n");
            }
        }

        void GetTargetMilestone()
        {
            targetMilestone = milestones.FirstOrDefault(x => x.Title == milestoneTitle);
            if (targetMilestone == null)
            {
                throw new Exception($"Could not find milestone for '{milestoneTitle}'.");
            }
        }
    }
}
