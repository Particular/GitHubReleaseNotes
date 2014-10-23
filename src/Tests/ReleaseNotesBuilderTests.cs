namespace ReleaseNotesCompiler.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Octokit;

    [TestFixture]
    public class ReleaseNotesBuilderTests
    {
        [Test]
        public void It_prints_number_of_isses_closed()
        {
            var fakeClient = new FakeGitHubClient();
            fakeClient.Issues.Add(CreateIssue(1, "Bug"));
            fakeClient.Issues.Add(CreateIssue(2, "Feature"));
            fakeClient.Issues.Add(CreateIssue(3, "Improvement"));

            fakeClient.Milestones.Add(CreateMilestone("1.2.3"));

            var builder = new ReleaseNotesBuilder(fakeClient, "SzymonPobiega", "FakeRepo", "1.2.3");

            var notes = builder.BuildReleaseNotes().Result;

            Assert.IsTrue(notes.Contains("3 issues"));
        }
        
        [Test]
        public void It_prints_number_of_commits_if_anything_has_been_committed()
        {
            var fakeClient = new FakeGitHubClient();
            fakeClient.Issues.Add(CreateIssue(1, "Bug"));
            fakeClient.Issues.Add(CreateIssue(2, "Feature"));
            fakeClient.Issues.Add(CreateIssue(3, "Improvement"));
            fakeClient.NumberOfCommits = 5;

            fakeClient.Milestones.Add(CreateMilestone("1.2.3"));

            var builder = new ReleaseNotesBuilder(fakeClient, "SzymonPobiega", "FakeRepo", "1.2.3");

            var notes = builder.BuildReleaseNotes().Result;

            Assert.IsTrue(notes.Contains("5 commits"));
        }
        
        [Test]
        public void It_does_not_print_number_of_issues_if_nothing_has_been_closed()
        {
            var fakeClient = new FakeGitHubClient();

            fakeClient.Milestones.Add(CreateMilestone("1.2.3"));

            var builder = new ReleaseNotesBuilder(fakeClient, "SzymonPobiega", "FakeRepo", "1.2.3");

            var notes = builder.BuildReleaseNotes().Result;

            Assert.IsFalse(notes.Contains("0 issues"));
        }

        [Test]
        public void It_does_not_print_number_of_commits_if_nothing_has_been_committed()
        {
            var fakeClient = new FakeGitHubClient();

            fakeClient.Milestones.Add(CreateMilestone("1.2.3"));

            var builder = new ReleaseNotesBuilder(fakeClient, "SzymonPobiega", "FakeRepo", "1.2.3");

            var notes = builder.BuildReleaseNotes().Result;

            Assert.IsFalse(notes.Contains("0 commits"));
        }

        static Milestone CreateMilestone(string version)
        {
            return new Milestone
                {
                    Title = version,
                    Url = new Uri("https://github.com/Particular/FakeRepo/issues?q=milestone%3A" + version)
                };
        }

        static Issue CreateIssue(int number, params string[] labels)
        {
            return new Issue
                {
                    Number = number,
                    Title = "Issue "+number,
                    HtmlUrl = new Uri("http://example.com/"+number),
                    Body = "Some issue",
                    Labels = labels.Select(x => new Label {Name = x}).ToArray(),
                };
        }
    }
}