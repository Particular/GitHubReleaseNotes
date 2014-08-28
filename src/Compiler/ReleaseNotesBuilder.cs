using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace ReleaseNotesCompiler
{
    using System.IO;

    public class ReleaseNotesBuilder
    {
        GitHubClient gitHubClient;
        string user;
        string repository;
        string milestoneTitle;
        List<Milestone> milestones;
        Milestone targetMilestone;

        public ReleaseNotesBuilder(GitHubClient gitHubClient, string user, string repository, string milestoneTitle)
        {
            this.gitHubClient = gitHubClient;
            this.user = user;
            this.repository = repository;
            this.milestoneTitle = milestoneTitle;
        }

        public async Task<string> BuildReleaseNotes()
        {
            await GetMilestones();

            GetTargetMilestone();
            var issues = await GetIssues(targetMilestone);
            var stringBuilder = new StringBuilder();
            var previousMilestone = GetPreviousMilestone();

            var issuesText = String.Format(issues.Count == 1 ? "{0} issue" : "{0} issues", issues.Count);

            var numberOfCommits = await GetNumberOfCommits(previousMilestone);
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
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(targetMilestone.Description);
            stringBuilder.AppendLine();

            AddIssues(stringBuilder, issues);

            await AddFooter(stringBuilder);

            return stringBuilder.ToString();
        }

        async Task<int> GetNumberOfCommits(Milestone previousMilestone)
        {
            try
            {
                if (previousMilestone == null)
                {
                    var gitHubClientRepositoryCommitsCompare = await gitHubClient.Repository.Commits.Compare(user, repository, "master", targetMilestone.Title);
                    return gitHubClientRepositoryCommitsCompare.AheadBy;
                }

                var compareResult = await gitHubClient.Repository.Commits.Compare(user, repository, previousMilestone.Title, "master");
                return compareResult.AheadBy;
            }
            catch (NotFoundException)
            {
                //If there is not tag yet the Compare will return a NotFoundException
                //we can safely ignore
                return 0;
            }
        }

        Milestone GetPreviousMilestone()
        {
            using (var orderedMilestones = milestones.OrderByDescending(x => x.Title, VersionComparer.Default).GetEnumerator())
            {
                Milestone previousMilestone = null;

                while (orderedMilestones.MoveNext())
                {
                    if (orderedMilestones.Current.Title == targetMilestone.Title)
                    {
                        break;
                    }
                }

                if (orderedMilestones.MoveNext())
                {
                    previousMilestone = orderedMilestones.Current;
                }
                return previousMilestone;
            }
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
            Append(issues, "Improvement", stringBuilder);
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
You can download this release from:
- Our [website](http://particular.net/downloads)
- Or [nuget](https://www.nuget.org/profiles/nservicebus/)");
                return;
            }

            using (var reader = file.OpenText())
            {
                stringBuilder.Append(await reader.ReadToEndAsync());
            }
        }

        async Task GetMilestones()
        {
            var milestonesClient = gitHubClient.Issue.Milestone;
            milestones = (await milestonesClient.GetForRepository(user, repository)).ToList();
        }

        async Task<List<Issue>> GetIssues(Milestone milestone)
        {
            var allIssues = await gitHubClient.AllIssuesForMilestone(milestone);
            var issues = new List<Issue>();
            foreach (var issue in allIssues.Where(x => !x.IsPullRequest() && x.State == ItemState.Closed))
            {
                CheckForValidLabels(issue);
                issues.Add(issue);
            }
            return issues;
        }

        void CheckForValidLabels(Issue issue)
        {
            var count = issue.Labels.Count(l =>
                l.Name == "Bug" ||
                l.Name == "Internal refactoring" ||
                l.Name == "Feature" ||
                l.Name == "Improvement");
            if (count != 1)
            {
                var message = string.Format("Bad Issue {0} expected to find a single label with either 'Bug', 'Internal refactoring', 'Improvement' or 'Feature'.", issue.HtmlUrl);
                throw new Exception(message);
            }
        }

        void Append(IEnumerable<Issue> issues, string label, StringBuilder stringBuilder)
        {
            var features = issues.Where(x => x.Labels.Any(l => l.Name == label))
                .ToList();
            if (features.Count > 0)
            {
                stringBuilder.AppendFormat("## {0}s\r\n\r\n", label);

                foreach (var issue in features)
                {
                    stringBuilder.AppendFormat("### [#{0} {1}]({2})\r\n\r\n{3}\r\n\r\n", issue.Number, issue.Title, issue.HtmlUrl, issue.ExtractSummary());
                }
                stringBuilder.AppendLine();
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