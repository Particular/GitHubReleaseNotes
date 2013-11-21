using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
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
    public async void SingleMilestone3()
    {
        var gitHubClient = ClientBuilder.Build();

        var releaseNotesBuilder = new ReleaseNotesBuilder(gitHubClient, "Particular", "ServiceControl", "1.0.0-Beta4");
        var result = await releaseNotesBuilder.BuildReleaseNotes();
        Debug.WriteLine(result);
        ClipBoardHelper.SetClipboard(result);
    }

    [Test]
    [Explicit]
    public void OctokitTests()
    {
        var gitHubClient = ClientBuilder.Build();
    }

  
}

[TestFixture]
public class ReleaseManagerTests
{
    [Test]
    [Explicit]
    public async void List_releases_that_needs_updates()
    {
        var gitHubClient = ClientBuilder.Build();

        var releaseNotesBuilder = new ReleaseManager(gitHubClient, "Particular");
        var result = await releaseNotesBuilder.GetReleasesInNeedOfUpdates();

        Debug.WriteLine("{0} releases found that needs updating",result.Count());
        foreach (var releaseName in result)
        {
            Debug.WriteLine(releaseName);
        }
       
    }  
}