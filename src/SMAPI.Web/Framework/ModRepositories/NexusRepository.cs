using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Clients.Nexus;

namespace StardewModdingAPI.Web.Framework.ModRepositories
{
    /// <summary>An HTTP client for fetching mod metadata from Nexus Mods.</summary>
    internal class NexusRepository : RepositoryBase
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying Nexus Mods API client.</summary>
        private readonly INexusClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="client">The underlying Nexus Mods API client.</param>
        public NexusRepository(INexusClient client)
            : base(ModRepositoryKey.Nexus)
        {
            this.Client = client;
        }

        /// <summary>Get metadata about a mod in the repository.</summary>
        /// <param name="id">The mod ID in this repository.</param>
        public override async Task<ModInfoModel> GetModInfoAsync(string id)
        {
            // validate ID format
            if (!uint.TryParse(id, out uint nexusID))
                return new ModInfoModel().SetError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid Nexus mod ID, must be an integer ID.");

            // fetch info
            try
            {
                NexusMod mod = await this.Client.GetModAsync(nexusID);
                if (mod == null)
                    return new ModInfoModel().SetError(RemoteModStatus.DoesNotExist, "Found no Nexus mod with this ID.");
                if (mod.Error != null)
                {
                    RemoteModStatus remoteStatus = mod.Status == NexusModStatus.Hidden || mod.Status == NexusModStatus.NotPublished
                        ? RemoteModStatus.DoesNotExist
                        : RemoteModStatus.TemporaryError;
                    return new ModInfoModel().SetError(remoteStatus, mod.Error);
                }

                return new ModInfoModel(name: mod.Name, version: this.NormaliseVersion(mod.Version), previewVersion: mod.LatestFileVersion?.ToString(), url: mod.Url);
            }
            catch (Exception ex)
            {
                return new ModInfoModel().SetError(RemoteModStatus.TemporaryError, ex.ToString());
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public override void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
