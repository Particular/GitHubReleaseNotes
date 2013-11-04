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

        public ReleaseNotesBuilder(GitHubClient gitHubClient, string user, string repository, string milestoneTitle)
        {
            this.gitHubClient = gitHubClient;
            this.user = user;
            this.repository = repository;
            this.milestoneTitle = milestoneTitle;
        }

        public async Task<string> BuildReleaseNotes()
        {
            var milestone = await GetMilestone();
            var issues = await GetIssues(milestone);
            var stringBuilder = new StringBuilder();
            Append(issues, "Bug", stringBuilder);
            Append(issues, "Feature", stringBuilder);
            Append(issues, "Improvement", stringBuilder);
            return stringBuilder.ToString();
        }

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


        async Task<Milestone> GetMilestone()
        {
            var milestones = await gitHubClient.Issue.Milestone.GetForRepository(user, repository);
            var milestone = milestones.FirstOrDefault(x => x.Title == milestoneTitle);
            if (milestone == null)
            {
                throw new Exception(string.Format("Could not find milestone for '{0}'.", milestoneTitle));
            }
            return milestone;
        }
    }
}