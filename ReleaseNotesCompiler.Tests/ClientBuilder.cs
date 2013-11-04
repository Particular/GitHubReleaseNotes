using System.Net.Http.Headers;
using Octokit;

public static class ClientBuilder
{
    public static GitHubClient Build()
    {
        return new GitHubClient(new ProductHeaderValue("ReleaseNotesCompiler"));
    }
}