using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Clients.CurseForge;

namespace StardewModdingAPI.Web.Framework.ModRepositories
{
    /// <summary>An HTTP client for fetching mod metadata from CurseForge.</summary>
    internal class CurseForgeRepository : RepositoryBase
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying CurseForge API client.</summary>
        private readonly ICurseForgeClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="client">The underlying CurseForge API client.</param>
        public CurseForgeRepository(ICurseForgeClient client)
            : base(ModRepositoryKey.CurseForge)
        {
            this.Client = client;
        }

        /// <summary>Get metadata about a mod in the repository.</summary>
        /// <param name="id">The mod ID in this repository.</param>
        public override async Task<ModInfoModel> GetModInfoAsync(string id)
        {
            // validate ID format
            if (!uint.TryParse(id, out uint curseID))
                return new ModInfoModel().SetError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid CurseForge mod ID, must be an integer ID.");

            // fetch info
            try
            {
                CurseForgeMod mod = await this.Client.GetModAsync(curseID);
                if (mod == null)
                    return new ModInfoModel().SetError(RemoteModStatus.DoesNotExist, "Found no CurseForge mod with this ID.");
                if (mod.Error != null)
                {
                    RemoteModStatus remoteStatus = RemoteModStatus.InvalidData;
                    return new ModInfoModel().SetError(remoteStatus, mod.Error);
                }

                return new ModInfoModel(name: mod.Name, version: this.NormalizeVersion(mod.LatestVersion), url: mod.Url);
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
