using System;
using MongoDB.Driver;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.ModRepositories;

namespace StardewModdingAPI.Web.Framework.Caching.Mods
{
    /// <summary>Encapsulates logic for accessing the mod data cache.</summary>
    internal class ModCacheRepository : BaseCacheRepository, IModCacheRepository
    {
        /*********
        ** Fields
        *********/
        /// <summary>The collection for cached mod data.</summary>
        private readonly IMongoCollection<CachedMod> Mods;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="database">The authenticated MongoDB database.</param>
        public ModCacheRepository(IMongoDatabase database)
        {
            // get collections
            this.Mods = database.GetCollection<CachedMod>("mods");

            // add indexes if needed
            this.Mods.Indexes.CreateOne(new CreateIndexModel<CachedMod>(Builders<CachedMod>.IndexKeys.Ascending(p => p.ID).Ascending(p => p.Site)));
        }

        /*********
        ** Public methods
        *********/
        /// <summary>Get the cached mod data.</summary>
        /// <param name="site">The mod site to search.</param>
        /// <param name="id">The mod's unique ID within the <paramref name="site"/>.</param>
        /// <param name="mod">The fetched mod.</param>
        /// <param name="markRequested">Whether to update the mod's 'last requested' date.</param>
        public bool TryGetMod(ModRepositoryKey site, string id, out CachedMod mod, bool markRequested = true)
        {
            // get mod
            id = this.NormaliseId(id);
            mod = this.Mods.Find(entry => entry.ID == id && entry.Site == site).FirstOrDefault();
            if (mod == null)
                return false;

            // bump 'last requested'
            if (markRequested)
            {
                mod.LastRequested = DateTimeOffset.UtcNow;
                mod = this.SaveMod(mod);
            }

            return true;
        }

        /// <summary>Save data fetched for a mod.</summary>
        /// <param name="site">The mod site on which the mod is found.</param>
        /// <param name="id">The mod's unique ID within the <paramref name="site"/>.</param>
        /// <param name="mod">The mod data.</param>
        /// <param name="cachedMod">The stored mod record.</param>
        public void SaveMod(ModRepositoryKey site, string id, ModInfoModel mod, out CachedMod cachedMod)
        {
            id = this.NormaliseId(id);

            cachedMod = this.SaveMod(new CachedMod(site, id, mod));
        }

        /// <summary>Delete data for mods which haven't been requested within a given time limit.</summary>
        /// <param name="age">The minimum age for which to remove mods.</param>
        public void RemoveStaleMods(TimeSpan age)
        {
            DateTimeOffset minDate = DateTimeOffset.UtcNow.Subtract(age);
            var result = this.Mods.DeleteMany(p => p.LastRequested < minDate);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Save data fetched for a mod.</summary>
        /// <param name="mod">The mod data.</param>
        public CachedMod SaveMod(CachedMod mod)
        {
            string id = this.NormaliseId(mod.ID);

            this.Mods.ReplaceOne(
                entry => entry.ID == id && entry.Site == mod.Site,
                mod,
                new UpdateOptions { IsUpsert = true }
            );

            return mod;
        }

        /// <summary>Normalise a mod ID for case-insensitive search.</summary>
        /// <param name="id">The mod ID.</param>
        public string NormaliseId(string id)
        {
            return id.Trim().ToLower();
        }
    }
}
