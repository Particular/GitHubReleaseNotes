namespace ReleaseNotesCompiler
{
    using System.Linq;
    using Octokit;

    static class IssueExtensions
    {
        public static bool IsBug(this Issue issue)
        {
            return issue.Labels.Any(label => label.Name == "Type: Bug" || label.Name == "Bug");
        }
    }
}