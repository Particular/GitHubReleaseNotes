using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;

namespace ReleaseNotesCompiler
{
    public class ReleaseManager
    {
        GitHubClient gitHubClient;
        string organization;

        public ReleaseManager(GitHubClient gitHubClient, string organization)
        {
            this.gitHubClient = gitHubClient;
            this.organization = organization;
        }

        public async Task<IEnumerable<string>> GetReleasesInNeedOfUpdates()
        {
            var repositories = await gitHubClient.Repository.GetAllForOrg(this.organization);


            var releases = new List<string>();

            foreach (var repository in repositories.Where(r => 
                r.Name != "NServiceBus" &&  //until we can patch octokit
                r.Name != "ServiceInsight" && //until we can patch octokit
                r.HasIssues))
            {
                Console.Out.WriteLine("Checking " + repository.Name);

                var milestones = await GetMilestones(repository.Name);

                var releasesForThisRepo = await gitHubClient.Release.GetAll(organization, repository.Name);

                foreach (var milestone in milestones)
                {
                    var potentialRelease = milestone.Title.Replace(" ", "");

                    var rel = releasesForThisRepo.SingleOrDefault(r => r.Name == potentialRelease);

                    if (rel != null)
                    {
                        Console.Out.WriteLine("Release exists for milestone " + potentialRelease);
                    }
                    else
                    {
                        Console.Out.WriteLine("No Release exists for milestone " + potentialRelease);

                        releases.Add(repository.Name + " - " + potentialRelease);
                    }

                }
                

            }

            return releases;
        }

        async Task<List<Milestone>> GetMilestones(string repository)
        {
            var milestonesClient = gitHubClient.Issue.Milestone;
            var openList = await milestonesClient.GetForRepository(organization, repository, new MilestoneRequest { State = ItemState.Open });

            return openList.ToList();
            //var closedList = await milestonesClient.GetForRepository(organization, repository, new MilestoneRequest { State = ItemState.Closed });
            //return openList.Union(closedList).ToList();
        }
    }
}