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
            var repositoryIssueRequest = new RepositoryIssueRequest
            {
                Milestone = milestone.Number.ToString()
            };
            var issues = await gitHubClient.Issue.GetForRepository(user, repository, repositoryIssueRequest);
            var list = issues.Where(x => !x.IsPullRequest()).ToList();

            CheckForBadIssues(list);

            var stringBuilder = new StringBuilder();
            Append(issues, "Bug", stringBuilder);
            Append(issues, "Feature", stringBuilder);
            Append(issues, "Improvement", stringBuilder);
            return stringBuilder.ToString();
        }

        void CheckForBadIssues(List<Issue> list)
        {
            foreach (var issue in list.Where(x => x.Labels.All(l => l.Name != "Bug" && l.Name != "Internal refactoring" && l.Name != "Feature" && l.Name != "Improvement")))
            {
                string foundLabelList;
                if (issue.Labels.Count == 0)
                {
                    foundLabelList = "none";
                }
                else
                {
                    foundLabelList = string.Format("'{0}'", string.Join("', '", issue.Labels.Select(x => x.Name)));
                }
                var message = string.Format("Bad Issue {0} expected to find a label with either 'Bug', 'Internal refactoring', 'Improvement' or 'Feature'. Instead found {1}.", HtmlUrl(issue), foundLabelList);
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
                    stringBuilder.AppendFormat("#### [#{0} {1}]({2})\r\n\r\n{3}\r\n\r\n", issue.Number, issue.Title, HtmlUrl(issue), issue.ExtractSummary());
                }
                stringBuilder.AppendLine();
            }
        }

        string HtmlUrl(Issue issue)
        {
//TODO: move back to HtmlUrl when https://github.com/octokit/octokit.net/issues/162 is fixed
            //var htmlUrl = issue.HtmlUrl;
            var htmlUrl = string.Format("https://github.com/{0}/{1}/issues/{2}", user, repository, issue.Number);
            return htmlUrl;
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