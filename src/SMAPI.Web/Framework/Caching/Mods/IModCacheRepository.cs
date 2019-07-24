using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.ModRepositories;

namespace StardewModdingAPI.Web.Framework.Caching.Mods
{
    /// <summary>Encapsulates logic for accessing the mod data cache.</summary>
    internal interface IModCacheRepository : ICacheRepository
    {
        /*********
        ** Methods
        *********/
        /// <summary>Get the cached mod data.</summary>
        /// <param name="site">The mod site to search.</param>
        /// <param name="id">The mod's unique ID within the <paramref name="site"/>.</param>
        /// <param name="mod">The fetched mod.</param>
        /// <param name="markRequested">Whether to update the mod's 'last requested' date.</param>
        bool TryGetMod(ModRepositoryKey site, string id, out CachedMod mod, bool markRequested = true);

        /// <summary>Save data fetched for a mod.</summary>
        /// <param name="site">The mod site on which the mod is found.</param>
        /// <param name="id">The mod's unique ID within the <paramref name="site"/>.</param>
        /// <param name="mod">The mod data.</param>
        /// <param name="cachedMod">The stored mod record.</param>
        void SaveMod(ModRepositoryKey site, string id, ModInfoModel mod, out CachedMod cachedMod);
    }
}
