using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Clients.ModDrop;

namespace StardewModdingAPI.Web.Framework.ModRepositories
{
    /// <summary>An HTTP client for fetching mod metadata from the ModDrop API.</summary>
    internal class ModDropRepository : RepositoryBase
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying ModDrop API client.</summary>
        private readonly IModDropClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="client">The underlying Nexus Mods API client.</param>
        public ModDropRepository(IModDropClient client)
            : base(ModRepositoryKey.ModDrop)
        {
            this.Client = client;
        }

        /// <summary>Get metadata about a mod in the repository.</summary>
        /// <param name="id">The mod ID in this repository.</param>
        public override async Task<ModInfoModel> GetModInfoAsync(string id)
        {
            // validate ID format
            if (!long.TryParse(id, out long modDropID))
                return new ModInfoModel().WithError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid ModDrop mod ID, must be an integer ID.");

            // fetch info
            try
            {
                ModDropMod mod = await this.Client.GetModAsync(modDropID);
                if (mod == null)
                    return new ModInfoModel().WithError(RemoteModStatus.DoesNotExist, "Found no ModDrop mod with this ID.");
                return new ModInfoModel(name: mod.Name, version: mod.LatestDefaultVersion?.ToString(), previewVersion: mod.LatestOptionalVersion?.ToString(), url: mod.Url);
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
