using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Clients.GitHub;

namespace StardewModdingAPI.Web.Framework.ModRepositories
{
    /// <summary>An HTTP client for fetching mod metadata from GitHub project releases.</summary>
    internal class GitHubRepository : RepositoryBase
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying GitHub API client.</summary>
        private readonly IGitHubClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="client">The underlying GitHub API client.</param>
        public GitHubRepository(IGitHubClient client)
            : base(ModRepositoryKey.GitHub)
        {
            this.Client = client;
        }

        /// <summary>Get metadata about a mod in the repository.</summary>
        /// <param name="id">The mod ID in this repository.</param>
        public override async Task<ModInfoModel> GetModInfoAsync(string id)
        {
            ModInfoModel result = new ModInfoModel().SetBasicInfo(id, $"https://github.com/{id}/releases");

            // validate ID format
            if (!id.Contains("/") || id.IndexOf("/", StringComparison.InvariantCultureIgnoreCase) != id.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase))
                return result.SetError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid GitHub mod ID, must be a username and project name like 'Pathoschild/LookupAnything'.");

            // fetch info
            try
            {
                // fetch repo info
                GitRepo repository = await this.Client.GetRepositoryAsync(id);
                if (repository == null)
                    return result.SetError(RemoteModStatus.DoesNotExist, "Found no GitHub repository for this ID.");
                result
                    .SetBasicInfo(repository.FullName, $"{repository.WebUrl}/releases")
                    .SetLicense(url: repository.License?.Url, name: repository.License?.Name);

                // get latest release (whether preview or stable)
                GitRelease latest = await this.Client.GetLatestReleaseAsync(id, includePrerelease: true);
                if (latest == null)
                    return result.SetError(RemoteModStatus.DoesNotExist, "Found no GitHub release for this ID.");

                // split stable/prerelease if applicable
                GitRelease preview = null;
                if (latest.IsPrerelease)
                {
                    GitRelease release = await this.Client.GetLatestReleaseAsync(id, includePrerelease: false);
                    if (release != null)
                    {
                        preview = latest;
                        latest = release;
                    }
                }

                // return data
                return result.SetVersions(version: this.NormaliseVersion(latest.Tag), previewVersion: this.NormaliseVersion(preview?.Tag));
            }
            catch (Exception ex)
            {
                return result.SetError(RemoteModStatus.TemporaryError, ex.ToString());
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public override void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
