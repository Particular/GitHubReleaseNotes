namespace ReleaseNotesCompiler.CLI
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Octokit;
    using FileMode = System.IO.FileMode;

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 6)
            {
                PrintHelp();
                return 1;
            }

            return MainAsync(args[0], args[1], args[2], args[3], args[4], args[5]).Result;
        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: ReleaseNotesBuilder.CLI <username> <password> <owner> <repo> <milestone> <asset>");
        }

        static async Task<int> MainAsync(string username, string password, string owner, string repository, string milestone, string asset)
        {
            try
            {
                var creds = new Credentials(username, password);
                var github = new GitHubClient(new ProductHeaderValue("ReleaseNotesCompiler")) { Credentials = creds };

                await CreateRelease(github, owner, repository, milestone, asset);

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 1;
            }
        }

        private static async Task CreateRelease(GitHubClient github, string owner, string repository, string milestone, string asset)
        {
            var releaseNotesBuilder = new ReleaseNotesBuilder(github, owner, repository, milestone);

            var result = await releaseNotesBuilder.BuildReleaseNotes();

            var releaseUpdate = new ReleaseUpdate(milestone)
            {
                Draft = true,
                Body = result,
                Name = milestone
            };
            var release = await github.Release.Create(owner, repository, releaseUpdate);

            var upload = new ReleaseAssetUpload { FileName = Path.GetFileName(asset), ContentType = "application/octet-stream", RawData = File.Open(asset, FileMode.Open) };

            await github.Release.UploadAsset(release, upload);
        }
    }
}