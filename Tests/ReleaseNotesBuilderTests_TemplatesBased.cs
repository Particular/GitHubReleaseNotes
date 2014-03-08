namespace ReleaseNotesCompiler.Tests
{
    using System.Diagnostics;
    using System.IO;
    using NUnit.Framework;
    using ReleaseNotesCompiler;

    /// <summary>
    /// Tests to make sure that the templates based replacement matche the current implementation.
    /// Once the template-based implementation is the only chosen as the default implementation,
    /// we should delete these tests as well as the Markdown files in `templateSupport`.
    /// </summary>
    [TestFixture]
    public class ReleaseNotesBuilderTests_TemplatesBased
    {
        [Test]
        //[Explicit]
        public async void SingleMilestone()
        {
            var gitHubClient = ClientBuilder.Build();

            var releaseNotesBuilder = new ReleaseNotesBuilder(gitHubClient, "Particular", "NServiceBus", "4.3.4");
            var result = await releaseNotesBuilder.BuildReleaseNotes();
            Debug.WriteLine(result);
            Assert.AreEqual(_originalSingleMileStone, result);
        }

        [Test]
        //[Explicit]
        public async void SingleMilestone3()
        {
            var gitHubClient = ClientBuilder.Build();

            var releaseNotesBuilder = new ReleaseNotesBuilder(gitHubClient, "Particular", "ServiceControl", "1.0.0-Beta4");
            var result = await releaseNotesBuilder.BuildReleaseNotes();
            Debug.WriteLine(result);
            Assert.AreEqual(_originalSingleMileStone3, result);
        }

        [Test]
        [Explicit]
        public void OctokitTests()
        {
            var gitHubClient = ClientBuilder.Build();
        }

        string _originalSingleMileStone = File.ReadAllText(@".\templateSupport\singleMilestone.md");
        string _originalSingleMileStone3 = File.ReadAllText(@".\templateSupport\singleMilestone3.md");

    }
}
