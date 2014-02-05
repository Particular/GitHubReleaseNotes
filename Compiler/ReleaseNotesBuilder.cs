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

            var stringBuilder = new StringBuilder();
            GetTargetMilestone();
            var commitsLink = GetCommitsLink();
            stringBuilder.AppendFormat(@"This release consist of [these issues]({0}) that were achieved through [these commits]({1}).", targetMilestone.HtmlUrl(),commitsLink);
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(targetMilestone.Description);
            stringBuilder.AppendLine();

            await AddIssues(stringBuilder);

            AddFooter(stringBuilder);


            var allText = stringBuilder.ToString();
            using (var reader = new StringReader(allText))
            {
                while (reader.Peek() >= 0)
                {
                    var readLine = reader.ReadLine();
                    if (readLine != null && readLine.StartsWith("#######"))
                    {
                        throw new Exception("After the issue has been nested under the top level headings a line has resulted in a 'too deep' headin level. The resulting line is \r\n"+readLine);
                    }
                }
            }
            return allText;
        }
        string GetCommitsLink()
        {
            
            var orderedMilestones = milestones.OrderByDescending(x => x.GetVersion());
            var previousMilestone = orderedMilestones.FirstOrDefault(x => x.DueOn < targetMilestone.DueOn);
            if (previousMilestone == null)
            {
                return string.Format("https://github.com/{0}/{1}/commits/{2}", user, repository, targetMilestone.Title);
            }
            return string.Format("https://github.com/{0}/{1}/compare/{2}...{3}", user, repository, previousMilestone.Title, targetMilestone.Title);
        }


        async Task AddIssues(StringBuilder stringBuilder)
        {
            var issues = await GetIssues(targetMilestone);
            Append(issues, "Feature", stringBuilder);
            Append(issues, "Improvement", stringBuilder);
            Append(issues, "Bug", stringBuilder);
        }

        static void AddFooter(StringBuilder stringBuilder)
        {
            stringBuilder.Append(@"## Where to get it
You can download this release from:
- Our [website](http://particular.net/downloads)
- Or [nuget](https://www.nuget.org/profiles/nservicebus/)");
        }

        async Task GetMilestones()
        {
            var milestonesClient = gitHubClient.Issue.Milestone;
            var openList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Open });
            var closedList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Closed });
            milestones = openList.Union(closedList).ToList();
        }

        async Task<List<Issue>> GetIssues(Milestone milestone)
        {
            var allIssues = await gitHubClient.AllIssuesForMilestone(milestone);
            var issues = new List<Issue>();
            foreach (var issue in allIssues.Where(x=>!x.IsPullRequest()))
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