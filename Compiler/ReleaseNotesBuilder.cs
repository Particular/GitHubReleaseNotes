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

        Milestone GetPreviousMilestone()
        {
            // GetVersion strips of prerelease stuff, resulting in an unreliable order if used for pre release versions.
            // That's probably why there was a DueOn sort earlier too.
            var orderedMilestones = milestones.OrderByDescending(x => x.GetVersion())
                                              .ThenByDescending(x => x.DueOn)
                                              .GetEnumerator();

            Milestone previousMilestone = null;

            while (orderedMilestones.MoveNext())
            {
                if (orderedMilestones.Current.Title == notes.targetMilestone.Title)
                {
                    break;
                }
            }

            if (orderedMilestones.MoveNext())
            {
                previousMilestone = orderedMilestones.Current;
            }
            return previousMilestone;
        }

        int GetNumberOfCommits(Milestone previousMilestone)
        {
            if (previousMilestone == null)
            {
                return gitHubClient.Repository.Commits.Compare(user, repository, "master", notes.targetMilestone.Title).Result.AheadBy;
            }

            return gitHubClient.Repository.Commits.Compare(user, repository, previousMilestone.Title, notes.targetMilestone.Title).Result.AheadBy;
        }

        async Task GetMilestones()
        {
            var milestonesClient = gitHubClient.Issue.Milestone;
            var openList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Open });
            var closedList = await milestonesClient.GetForRepository(user, repository, new MilestoneRequest { State = ItemState.Closed });
            milestones = openList.Union(closedList).ToList();
        }

        async Task GetIssues()
        {
            _issues = await GetIssues(notes.targetMilestone);
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

        void SetMilestoneData()
        {
            notes.targetMilestone = GetTargetMilestone(notes.milestoneTitle);

            var previousMilestone = GetPreviousMilestone();
            var numberOfCommits = GetNumberOfCommits(previousMilestone);
            notes.commitsText = String.Format(numberOfCommits > 1 ? "{0} commits" : "{0} commit", numberOfCommits);
            
            notes.commitsLink = GetCommitsLink(previousMilestone);
            notes.targetMilestoneHtmlUrl = notes.targetMilestone.HtmlUrl();
        }

        void SetIssueData()
        {
            Append(_issues, "Feature");
            Append(_issues, "Improvement");
            Append(_issues, "Bug");

            notes.issuesText = String.Format(_issues.Count > 1 ? "{0} issues" : "{0} issue", _issues.Count);
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

        string GetCommitsLink(Milestone previousMilestone)
        {
            if (previousMilestone == null)
            {
                return string.Format("https://github.com/{0}/{1}/commits/{2}", user, repository, notes.targetMilestone.Title);
            }
            return string.Format("https://github.com/{0}/{1}/compare/{2}...{3}", user, repository, previousMilestone.Title, notes.targetMilestone.Title);
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

        Milestone GetTargetMilestone(string milestoneTitle)
        {
            var targetMilestone = milestones.FirstOrDefault(x => x.Title == milestoneTitle);
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
            public string commitsText;
            public string issuesText;

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