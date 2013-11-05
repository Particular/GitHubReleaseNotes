using System;
using System.Net.Http.Headers;
using Octokit;

public static class ClientBuilder
{
    public static GitHubClient Build()
    {


        var githubUsername = Environment.GetEnvironmentVariable("OCTOKIT_GITHUBUSERNAME");
        var githubPassword = Environment.GetEnvironmentVariable("OCTOKIT_GITHUBPASSWORD");

        if (githubUsername == null || githubPassword == null)
        {
            throw new Exception("expected OCTOKIT_GITHUBUSERNAME and OCTOKIT_GITHUBPASSWORD env variables to exist");
        }

        return new GitHubClient(new ProductHeaderValue("ReleaseNotesCompiler"))
        {
            Credentials = new Credentials(githubUsername, githubPassword)
        };
    }
}
