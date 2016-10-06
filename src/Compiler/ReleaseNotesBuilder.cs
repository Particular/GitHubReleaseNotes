using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace ReleaseNotesCompiler
{
    using static System.String;

    public class ReleaseNotesBuilder
    {
        public ReleaseNotesBuilder(IGitHubClient gitHubClient, string user, string repository, string milestoneTitle)
        {
            this.gitHubClient = gitHubClient;
            this.user = user;
            this.repository = repository;
            this.milestoneTitle = milestoneTitle;
        }

        public static string LabelPrefix => "Type: ";

        public async Task<string> BuildReleaseNotes()
        {
            var milestones = await gitHubClient.GetMilestones();

            var targetMilestone = milestones.FirstOrDefault(x => x.Title == milestoneTitle);

            if (targetMilestone == null)
            {
                throw new Exception($"Could not find milestone for '{milestoneTitle}'.");
            }
            var issues = await gitHubClient.GetIssues(targetMilestone);
            var stringBuilder = new StringBuilder();
            var previousMilestone = GetPreviousMilestone(targetMilestone, milestones);
            var numberOfCommits = await gitHubClient.GetNumberOfCommitsBetween(previousMilestone, targetMilestone);

            if (issues.Count > 0)
            {
                var issuesText = Format(issues.Count == 1 ? "{0} issue" : "{0} issues", issues.Count);

                if (numberOfCommits > 0)
                {
                    var commitsLink = GetCommitsLink(targetMilestone, previousMilestone);
                    var commitsText = Format(numberOfCommits == 1 ? "{0} commit" : "{0} commits", numberOfCommits);

                    stringBuilder.Append($"As part of this release we had [{commitsText}]({commitsLink}) which resulted in [{issuesText}]({targetMilestone.HtmlUrl()}) being closed.");
                }
                else
                {
                    stringBuilder.Append($"As part of this release we had [{issuesText}]({targetMilestone.HtmlUrl()}) closed.");
                }
            }
            else if (numberOfCommits > 0)
            {
                var commitsLink = GetCommitsLink(targetMilestone, previousMilestone);
                var commitsText = Format(numberOfCommits == 1 ? "{0} commit" : "{0} commits", numberOfCommits);
                stringBuilder.Append($"As part of this release we had [{commitsText}]({commitsLink}).");
            }
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(targetMilestone.Description);
            stringBuilder.AppendLine();

            AddIssues(stringBuilder, issues);

            await AddFooter(stringBuilder);

            return stringBuilder.ToString();
        }

        Milestone GetPreviousMilestone(Milestone targetMilestone, IReadOnlyList<Milestone> milestones)
        {
            var currentVersion = targetMilestone.Version();
            return milestones
                .OrderByDescending(m => m.Version())
                .Distinct().ToList()
                .SkipWhile(x => x.Version() >= currentVersion)
                .FirstOrDefault();
        }

        string GetCommitsLink(Milestone targetMilestone, Milestone previousMilestone)
        {
            if (previousMilestone == null)
            {
                return $"https://github.com/{user}/{repository}/commits/{targetMilestone.Title}";
            }
            return $"https://github.com/{user}/{repository}/compare/{previousMilestone.Title}...{targetMilestone.Title}";
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

            var others = issues.Where(issue => !issue.Labels.Any() || issue.Labels.Any(label => label.Name != "Type: Refactoring" && label.Name != "Type: Bug"))
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

        IGitHubClient gitHubClient;
        string user;
        string repository;
        string milestoneTitle;
    }
}
