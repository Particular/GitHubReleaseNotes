using System;
using System.Collections.Generic;
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
       // List<Milestone> milestones;
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
            //await GetMilestones();

            var stringBuilder = new StringBuilder();
            targetMilestone = await GetTargetMilestone();
            //var commitsLink = GetCommitsLink();
            //stringBuilder.AppendFormat(@"To see the full list of commits for this release click [here]({0}).", commitsLink);
            //stringBuilder.AppendLine();

            stringBuilder.AppendFormat(@"To see the full list of issues see [Milestone {0}](https://github.com/{1}/{2}/issues?milestone={3}&page=1&state=closed).", targetMilestone.Title, user, repository, targetMilestone.Number);

            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(targetMilestone.Description);
            stringBuilder.AppendLine();
            var issues = await GetIssues(targetMilestone);
            Append(issues, "Feature", stringBuilder);
            Append(issues, "Improvement", stringBuilder);
            Append(issues, "Bug", stringBuilder);

            stringBuilder.Append(@"## Where to get it
You can download this release from:
- Our [website](http://particular.net/downloads)
- Or [nuget](https://www.nuget.org/profiles/nservicebus/)");
            return stringBuilder.ToString();
        }

        //async Task GetMilestones()
        //{
        //    var milestonesClient = gitHubClient.Issue.Milestone;
        //    var openList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Open });
        //    var closedList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Closed });
        //    milestones = openList.Union(closedList).ToList();
        //}

        //string GetCommitsLink()
        //{
        //    var previousMilestone = milestones.FirstOrDefault(x => x.DueOn < targetMilestone.DueOn);
        //    if (previousMilestone == null)
        //    {
        //        return string.Format("https://github.com/{0}/{1}/commits/{2}", user, repository, targetMilestone.Title);
        //    }
        //    return string.Format("https://github.com/{0}/{1}/compare/{2}...{3}", user, repository, previousMilestone.Title, targetMilestone.Title);
        //}

        async Task<List<Issue>> GetIssues(Milestone milestone)
        {
            var closedIssueRequest = new RepositoryIssueRequest
            {
                Milestone = milestone.Number.ToString(),
                State = ItemState.Closed
            };
            var openIssueRequest = new RepositoryIssueRequest
            {
                Milestone = milestone.Number.ToString(),
                State = ItemState.Open
            };
            var closedIssues = await gitHubClient.Issue.GetForRepository(user, repository, closedIssueRequest);
            var openIssues = await gitHubClient.Issue.GetForRepository(user, repository, openIssueRequest);

            var issues = new List<Issue>();
            foreach (var issue in openIssues.Union(closedIssues).Where(x=>!x.IsPullRequest()))
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
                var message = string.Format("Bad Issue {0} expected to find a label with either 'Bug', 'Internal refactoring', 'Improvement' or 'Feature'.", HtmlUrl(issue));
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
                    stringBuilder.AppendFormat("### [#{0} {1}]({2})\r\n\r\n{3}\r\n\r\n", issue.Number, issue.Title, HtmlUrl(issue), issue.ExtractSummary());
                }
                stringBuilder.AppendLine();
            }
        }

        string HtmlUrl(Issue issue)
        {
//TODO: move back to HtmlUrl when https://github.com/octokit/octokit.net/issues/162 is fixed
            //var htmlUrl = issue.HtmlUrl;
            return string.Format("https://github.com/{0}/{1}/issues/{2}", user, repository, issue.Number);
        }


        async Task<Milestone> GetTargetMilestone()
        {

            var milestonesClient = gitHubClient.Issue.Milestone;
            var openList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Open });
            var milestone = openList.FirstOrDefault(x => x.Title == milestoneTitle);
            if (milestone == null)
            {
                throw new Exception(string.Format("Could not find milestone for '{0}'.", milestoneTitle));
            }
            return milestone;
        }
    }
}