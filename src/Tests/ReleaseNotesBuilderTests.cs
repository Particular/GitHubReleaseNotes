namespace ReleaseNotesCompiler.Tests
{
    using System.Diagnostics;
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

            var releaseNotesBuilder = new ReleaseNotesBuilder(gitHubClient, "Particular", "NServiceBus", "4.6.5");
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
}
