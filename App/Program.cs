namespace ReleaseNotesCompiler.CLI
{
    using System;
    using System.Threading.Tasks;
    using Octokit;

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 5)
            {
                PrintHelp();
                return 1;
            }

            return MainAsync(args[0], args[1], args[2], args[3], args[4]).Result;
        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: ReleaseNotesBuilder.CLI <username> <password> <owner> <repo> <milestone>");
        }

        static async Task<int> MainAsync(string username, string password, string owner, string repository, string milestone)
        {
            try
            {
                var creds = new Credentials(username, password);
                var github = new GitHubClient(new ProductHeaderValue("ReleaseNotesCompiler")) { Credentials = creds };

                await CreateRelease(github, owner, repository, milestone);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static async Task CreateRelease(GitHubClient github, string owner, string repository, string milestone)
        {
            var releaseNotesBuilder = new ReleaseNotesBuilder(github, owner, repository, milestone);

            var result = await releaseNotesBuilder.BuildReleaseNotes();

            var releaseUpdate = new ReleaseUpdate(milestone)
            {
                Draft = false,
                Body = result,
                Name = string.Format("{0} {1}", repository, milestone)
            };
            await github.Release.CreateRelease(owner, repository, releaseUpdate);
        }
    }
}