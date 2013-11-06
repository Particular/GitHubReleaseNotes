using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace ReleaseNotesCompiler
{
    public static class OctokitExtensions
    {
        public static bool IsPullRequest(this Issue issue)
        {
            return issue.PullRequest != null;
        }
        public static async Task<IEnumerable<Issue>> AllIssuesForMilestone(this GitHubClient gitHubClient, Milestone milestone)
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
            var parts = milestone.Url.AbsolutePath.Split('/');
            var user = parts[2];
            var repository = parts[3];
            var closedIssues = await gitHubClient.Issue.GetForRepository(user, repository, closedIssueRequest);
            var openIssues = await gitHubClient.Issue.GetForRepository(user, repository, openIssueRequest);
            return openIssues.Union(closedIssues);
        }

        public static string HtmlUrl(this  Milestone milestone)
        {
            var parts = milestone.Url.AbsolutePath.Split('/');
            var user = parts[2];
            var repository = parts[3];
            return string.Format("https://github.com/{0}/{1}/issues?milestone={2}&state=closed", user, repository, milestone.Number);
        }

        
        public static string ExtractSummary(this Issue issue)
        {
            IEnumerable<string> lines = issue.Body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);


            lines = FixHeaders(lines);


            var builder = new StringBuilder();
            if (lines.Any(x => x.StartsWith("--")))
            {
                var previousIsEmpty = true;
                foreach (var line in lines)
                {
                    if (previousIsEmpty)
                    {
                        if (line == "--")
                        {
                            break;
                        }
                    }
                    builder.AppendLine(line);

                    previousIsEmpty = string.IsNullOrWhiteSpace(line);
                }
            }
            else
            {
                var count = 0;
                var inCode = false;
                foreach (var line in lines)
                {
                    if (line.StartsWith("```"))
                    {
                        inCode = !inCode;
                    }
                    builder.AppendLine(line);
                    if (count == 10)
                    {
                        if (inCode)
                        {
                            builder.Append("```\r\n\r\n");
                        }
                        builder.AppendFormat("*Content trimmed. See [full issue]({0})*", issue.HtmlUrl);
                        break;
                    }

                    count++;
                }
            }
            return builder.ToString();
        }

        static IEnumerable<string> FixHeaders(IEnumerable<string> lines)
        {
            var inCode = false;
            foreach (var line in lines)
            {

                if (line.StartsWith("```"))
                {
                    inCode = !inCode;
                }
                if (!inCode && line.StartsWith("#"))
                {
                    yield return "###" + line;
                }
                else
                {
                    yield return line;    
                }
            }
        }
    }
}