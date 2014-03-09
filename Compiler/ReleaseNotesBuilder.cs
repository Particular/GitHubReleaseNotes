using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using Nustache.Core;

namespace ReleaseNotesCompiler
{

    public class ReleaseNotesBuilder
    {
        GitHubClient gitHubClient;
        string user;
        string repository;

        List<Milestone> milestones;
        Milestone targetMilestone;

        ReleaseNotes notes;
        List<Issue> _issues;

        public ReleaseNotesBuilder(GitHubClient gitHubClient, string user, string repository, string milestoneTitle)
        {
            this.gitHubClient = gitHubClient;
            this.user = user;
            this.repository = repository;
            notes = new ReleaseNotes() { milestoneTitle = milestoneTitle };
        }

        public async Task<string> BuildReleaseNotes()
        {
            await GetMilestones();

            SetMilestoneData();

            await GetIssues();

            SetIssueData();

            var markdown = Render.FileToString(@".\templates\particular.md.template", notes);

            ValidMarkdownOrThrow(markdown);

            return markdown;
        }

        void SetIssueData()
        {
            Append(_issues, "Feature");
            Append(_issues, "Improvement");
            Append(_issues, "Bug");
        }

        static void ValidMarkdownOrThrow(string markdown)
        {
            using (var reader = new StringReader(markdown))
            {
                while (reader.Peek() >= 0)
                {
                    var readLine = reader.ReadLine();
                    if (readLine != null && readLine.StartsWith("#######"))
                    {
                        throw new Exception("After the issue has been nested under the top level headings a line has resulted in a 'too deep' headin level. The resulting line is \r\n" + readLine);
                    }
                }
            }
        }

        void SetMilestoneData()
        {
            notes.targetMilestone = GetTargetMilestone(notes.milestoneTitle);
            notes.commitsLink = GetCommitsLink();
            notes.targetMilestoneHtmlUrl = targetMilestone.HtmlUrl();
        }

        string GetCommitsLink()
        {

            var orderedMilestones = milestones.OrderByDescending(x => x.GetVersion());
            var previousMilestone = orderedMilestones.FirstOrDefault(x => x.DueOn < targetMilestone.DueOn);
            if (previousMilestone == null)
            {
                return string.Format("https://github.com/{0}/{1}/commits/{2}", user, repository, targetMilestone.Title);
            }
            return string.Format("https://github.com/{0}/{1}/compare/{2}...{3}", user, repository, previousMilestone.Title, targetMilestone.Title);
        }

        async Task GetIssues()
        {
            _issues = await GetIssues(targetMilestone);
        }

        async Task GetMilestones()
        {
            var milestonesClient = gitHubClient.Issue.Milestone;
            var openList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Open });
            var closedList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Closed });
            milestones = openList.Union(closedList).ToList();
        }

        async Task<List<Issue>> GetIssues(Milestone milestone)
        {
            var allIssues = await gitHubClient.AllIssuesForMilestone(milestone);
            var issues = new List<Issue>();
            foreach (var issue in allIssues.Where(x => !x.IsPullRequest()))
            {
                CheckForValidLabels(issue);
                issues.Add(issue);
            }
            return issues;
        }

        void CheckForValidLabels(Issue issue)
        {
            var count = issue.Labels.Count(l =>
                l.Name == "Bug" ||
                l.Name == "Internal refactoring" ||
                l.Name == "Feature" ||
                l.Name == "Improvement");
            if (count != 1)
            {
                var message = string.Format("Bad Issue {0} expected to find a single label with either 'Bug', 'Internal refactoring', 'Improvement' or 'Feature'.", issue.HtmlUrl);
                throw new Exception(message);
            }
        }

        void Append(IEnumerable<Issue> issues, string label)
        {
            var features = issues.Where(x => x.Labels.Any(l => l.Name == label))
                .ToArray();

            if (features.Any())
            {
                notes.AddIssue(label, 
                    features.Select(f => new IssueWrapper(f)).ToArray());
            }
        }

        Milestone GetTargetMilestone(string milestoneTitle)
        {
            targetMilestone = milestones.FirstOrDefault(x => x.Title == milestoneTitle);
            if (targetMilestone == null)
            {
                throw new Exception(string.Format("Could not find milestone for '{0}'.", milestoneTitle));
            }
            return targetMilestone;
        }

        #region release note and issue data classes
        internal class ReleaseNotes
        {
            public string milestoneTitle;
            public Milestone targetMilestone;
            public string commitsLink;
            public string targetMilestoneHtmlUrl;

            public List<IssueGroup> issuesByLabel = new List<IssueGroup>();

            public void AddIssue(string label, IssueWrapper[] issues)
            {
                issuesByLabel.Add(new IssueGroup { label = label, issues = issues });
            }
        }

        internal class IssueGroup
        {
            public string label;
            public IssueWrapper[] issues;
        }

        internal class IssueWrapper
        {
            public readonly Issue issue;

            public IssueWrapper(Issue issue)
            {
                this.issue = issue;
            }

            public string summary
            {
                get { return issue.ExtractSummary(); }
            }
        }
        #endregion
    }
}