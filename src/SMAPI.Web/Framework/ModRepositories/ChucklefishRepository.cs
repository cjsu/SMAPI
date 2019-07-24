using System;
using System.Threading.Tasks;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Clients.Chucklefish;

namespace StardewModdingAPI.Web.Framework.ModRepositories
{
    /// <summary>An HTTP client for fetching mod metadata from the Chucklefish mod site.</summary>
    internal class ChucklefishRepository : RepositoryBase
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying HTTP client.</summary>
        private readonly IChucklefishClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="client">The underlying HTTP client.</param>
        public ChucklefishRepository(IChucklefishClient client)
            : base(ModRepositoryKey.Chucklefish)
        {
            this.Client = client;
        }

        /// <summary>Get metadata about a mod in the repository.</summary>
        /// <param name="id">The mod ID in this repository.</param>
        public override async Task<ModInfoModel> GetModInfoAsync(string id)
        {
            // validate ID format
            if (!uint.TryParse(id, out uint realID))
                return new ModInfoModel().WithError(RemoteModStatus.DoesNotExist, $"The value '{id}' isn't a valid Chucklefish mod ID, must be an integer ID.");

            // fetch info
            try
            {
                var mod = await this.Client.GetModAsync(realID);
                if (mod == null)
                    return new ModInfoModel().WithError(RemoteModStatus.DoesNotExist, "Found no Chucklefish mod with this ID.");

                // create model
                return new ModInfoModel(name: mod.Name, version: this.NormaliseVersion(mod.Version), url: mod.Url);
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
