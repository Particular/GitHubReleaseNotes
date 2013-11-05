using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using NUnit.Framework;
using Octokit;
using ReleaseNotesCompiler;

[TestFixture]
public class ReleaseNotesBuilderTests
{
    [Test]
    [Explicit]
    public async void SingleMilestone()
    {
        var gitHubClient = ClientBuilder.Build();

        var releaseNotesBuilder = new ReleaseNotesBuilder(gitHubClient, "Particular", "NServiceBus","4.2.0");
        var result = await releaseNotesBuilder.BuildReleaseNotes();
        Debug.WriteLine(result);
        ClipBoardHelper.SetClipboard(result);
    }
    [Test]
    [Explicit]
    public async void SingleMilestone2()
    {
        var gitHubClient = ClientBuilder.Build();

        var releaseNotesBuilder = new ReleaseNotesBuilder(gitHubClient, "Particular", "NServiceBus","4.3.0");
        var result = await releaseNotesBuilder.BuildReleaseNotes();
        Debug.WriteLine(result);
        ClipBoardHelper.SetClipboard(result);
    }
    [Test]
    [Explicit]
    public async void OctokitTests()
    {
        var gitHubClient = ClientBuilder.Build();
    }
}