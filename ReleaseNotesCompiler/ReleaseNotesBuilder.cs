using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace ReleaseNotesCompiler
{
    public class ReleaseNotesBuilder
    {
        public async Task<string> BuildReleaseNotes(GitHubClient gitHubClient, string user, string repository)
        {

            var repositoryIssueRequest = new RepositoryIssueRequest
            {
                State = ItemState.Closed,
                Milestone = "39"
            };
            var issues = await gitHubClient.Issue.GetForRepository("Particular", "NServiceBus", repositoryIssueRequest);
            var list = issues.Where(x => !x.IsPullRequest()).ToList();

            foreach (var issue in list.Where(x => x.Labels.All(l => l.Name != "Bug" && l.Name != "Internal refactoring" && l.Name != "Feature")))
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
                var message = string.Format("Bad Issue {0} expected to find a label with either 'Bug', 'Internal refactoring' or 'Feature'. Instead found {1}.", issue.HtmlUrl, foundLabelList);
                throw new Exception(message);
            }

            var stringBuilder = new StringBuilder();

            var features = issues.Where(x => x.Labels.Any(l => l.Name == "Feature"))
                .ToList();
            if (features.Count > 0)
            {
                stringBuilder.Append("## New Features\r\n\r\n");

                foreach (var issue in features)
                {
                    AppendIssue(stringBuilder, issue);
                }
                stringBuilder.AppendLine();
            }

            var bugs = issues.Where(x => x.Labels.Any(l => l.Name == "Bug"))
                .ToList();
            if (bugs.Count > 0)
            {
                stringBuilder.Append("## Bugs\r\n\r\n");
                foreach (var issue in bugs)
                {
                    AppendIssue(stringBuilder, issue);
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }


        static void AppendIssue(StringBuilder stringBuilder, Issue issue)
        {
            var body = ExtractBody(issue);
            stringBuilder.AppendFormat("#### [#{0} {1}]({2})\r\n\r\n{3}\r\n\r\n", issue.Number, issue.Title, issue.HtmlUrl, body);
        }

        static string ExtractBody(Issue issue)
        {
            var lines = issue.Body.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);

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
                        builder.AppendFormat("Content trimmed. See [full issue]({0})", issue.HtmlUrl);
                        break;
                    }

                    count++;
                }
            }
            var body = builder.ToString();
            return body;
        }
    }
}