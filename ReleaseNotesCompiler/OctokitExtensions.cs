using Octokit;

namespace ReleaseNotesCompiler
{
    public static class OctokitExtensions
    {
        public static bool IsPullRequest(this Issue issue)
        {
            return issue.PullRequest.HtmlUrl != null;
        }
    }
}