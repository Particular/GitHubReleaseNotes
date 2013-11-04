using System.Diagnostics;
using NUnit.Framework;
using ReleaseNotesCompiler;

[TestFixture]
public class ReleaseNotesBuilderTests
{
    [Test]
    [Explicit]
    public async void Foo()
    {
        var gitHubClient = ClientBuilder.Build();

        var releaseNotesBuilder = new ReleaseNotesBuilder();
        var result = await releaseNotesBuilder.BuildReleaseNotes(gitHubClient, "Particular", "NServiceBus");
        Debug.WriteLine(result);
        ClipBoardHelper.SetClipboard(result);
    }
}