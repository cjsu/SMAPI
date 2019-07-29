using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Pathoschild.Http.Client;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub
{
    /// <summary>An HTTP client for fetching metadata from GitHub.</summary>
    internal class GitHubClient : IGitHubClient
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying HTTP client.</summary>
        private readonly IClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="baseUrl">The base URL for the GitHub API.</param>
        /// <param name="userAgent">The user agent for the API client.</param>
        /// <param name="acceptHeader">The Accept header value expected by the GitHub API.</param>
        /// <param name="username">The username with which to authenticate to the GitHub API.</param>
        /// <param name="password">The password with which to authenticate to the GitHub API.</param>
        public GitHubClient(string baseUrl, string userAgent, string acceptHeader, string username, string password)
        {
            this.Client = new FluentClient(baseUrl)
                .SetUserAgent(userAgent)
                .AddDefault(req => req.WithHeader("Accept", acceptHeader));
            if (!string.IsNullOrWhiteSpace(username))
                this.Client = this.Client.SetBasicAuthentication(username, password);
        }

        /// <summary>Get basic metadata for a GitHub repository, if available.</summary>
        /// <param name="repo">The repository key (like <c>Pathoschild/SMAPI</c>).</param>
        /// <returns>Returns the repository info if it exists, else <c>null</c>.</returns>
        public async Task<GitRepo> GetRepositoryAsync(string repo)
        {
            this.AssertKeyFormat(repo);
            try
            {
                return await this.Client
                    .GetAsync($"repos/{repo}")
                    .As<GitRepo>();
            }
            catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>Get the latest release for a GitHub repository.</summary>
        /// <param name="repo">The repository key (like <c>Pathoschild/SMAPI</c>).</param>
        /// <param name="includePrerelease">Whether to return a prerelease version if it's latest.</param>
        /// <returns>Returns the release if found, else <c>null</c>.</returns>
        public async Task<GitRelease> GetLatestReleaseAsync(string repo, bool includePrerelease = false)
        {
            this.AssertKeyFormat(repo);
            try
            {
                if (includePrerelease)
                {
                    GitRelease[] results = await this.Client
                        .GetAsync($"repos/{repo}/releases?per_page=2") // allow for draft release (only visible if GitHub repo is owned by same account as the update check credentials)
                        .AsArray<GitRelease>();
                    return results.FirstOrDefault(p => !p.IsDraft);
                }

                return await this.Client
                    .GetAsync($"repos/{repo}/releases/latest")
                    .As<GitRelease>();
            }
            catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            this.Client?.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Assert that a repository key is formatted correctly.</summary>
        /// <param name="repo">The repository key (like <c>Pathoschild/SMAPI</c>).</param>
        /// <exception cref="ArgumentException">The repository key is invalid.</exception>
        private void AssertKeyFormat(string repo)
        {
            if (repo == null || !repo.Contains("/") || repo.IndexOf("/", StringComparison.InvariantCultureIgnoreCase) != repo.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException($"The value '{repo}' isn't a valid GitHub repository key, must be a username and project name like 'Pathoschild/SMAPI'.", nameof(repo));
        }
    }
}
