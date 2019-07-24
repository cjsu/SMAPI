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
            // validate ID format
            if (!id.Contains("/") || id.IndexOf("/", StringComparison.InvariantCultureIgnoreCase) != id.LastIndexOf("/", StringComparison.InvariantCultureIgnoreCase))
                return new ModInfoModel().WithError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid GitHub mod ID, must be a username and project name like 'Pathoschild/LookupAnything'.");

            // fetch info
            try
            {
                // get latest release (whether preview or stable)
                GitRelease latest = await this.Client.GetLatestReleaseAsync(id, includePrerelease: true);
                if (latest == null)
                    return new ModInfoModel().WithError(RemoteModStatus.DoesNotExist, "Found no GitHub release for this ID.");

                // split stable/prerelease if applicable
                GitRelease preview = null;
                if (latest.IsPrerelease)
                {
                    GitRelease result = await this.Client.GetLatestReleaseAsync(id, includePrerelease: false);
                    if (result != null)
                    {
                        preview = latest;
                        latest = result;
                    }
                }

                // return data
                return new ModInfoModel(name: id, version: this.NormaliseVersion(latest.Tag), previewVersion: this.NormaliseVersion(preview?.Tag), url: $"https://github.com/{id}/releases");
            }
            catch (Exception ex)
            {
                return new ModInfoModel().WithError(RemoteModStatus.TemporaryError, ex.ToString());
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public override void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
