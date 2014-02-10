namespace ReleaseNotesCompiler.Tests
{
    using System.Net.Http.Headers;
    using Octokit;
    using Octokit.Internal;

    public static class ClientBuilder
    {
        public static GitHubClient Build()
        {
            var credentialStore = new InMemoryCredentialStore(Helper.Credentials);

            var httpClient = new HttpClientAdapter(Helper.Proxy);

            var connection = new Connection(
                new ProductHeaderValue("ReleaseNotesCompiler"),
                GitHubClient.GitHubApiUrl,
                credentialStore,
                httpClient,
                new SimpleJsonSerializer());

            var client = new GitHubClient(connection);

            return client;
        }
    }
}
