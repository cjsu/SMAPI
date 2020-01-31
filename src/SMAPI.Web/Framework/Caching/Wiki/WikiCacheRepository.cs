using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Driver;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

namespace StardewModdingAPI.Web.Framework.Caching.Wiki
{
    /// <summary>Encapsulates logic for accessing the wiki data cache.</summary>
    internal class WikiCacheRepository : BaseCacheRepository, IWikiCacheRepository
    {
        /*********
        ** Fields
        *********/
        /// <summary>The collection for wiki metadata.</summary>
        private readonly IMongoCollection<CachedWikiMetadata> WikiMetadata;

        /// <summary>The collection for wiki mod data.</summary>
        private readonly IMongoCollection<CachedWikiMod> WikiMods;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="database">The authenticated MongoDB database.</param>
        public WikiCacheRepository(IMongoDatabase database)
        {
            // get collections
            this.WikiMetadata = database.GetCollection<CachedWikiMetadata>("wiki-metadata");
            this.WikiMods = database.GetCollection<CachedWikiMod>("wiki-mods");

            // add indexes if needed
            this.WikiMods.Indexes.CreateOne(new CreateIndexModel<CachedWikiMod>(Builders<CachedWikiMod>.IndexKeys.Ascending(p => p.ID)));
        }

        /// <summary>Get the cached wiki metadata.</summary>
        /// <param name="metadata">The fetched metadata.</param>
        public bool TryGetWikiMetadata(out CachedWikiMetadata metadata)
        {
            metadata = this.WikiMetadata.Find("{}").FirstOrDefault();
            return metadata != null;
        }

        /// <summary>Get the cached wiki mods.</summary>
        /// <param name="filter">A filter to apply, if any.</param>
        public IEnumerable<CachedWikiMod> GetWikiMods(Expression<Func<CachedWikiMod, bool>> filter = null)
        {
            return filter != null
                ? this.WikiMods.Find(filter).ToList()
                : this.WikiMods.Find("{}").ToList();
        }

        /// <summary>Save data fetched from the wiki compatibility list.</summary>
        /// <param name="stableVersion">The current stable Stardew Valley version.</param>
        /// <param name="betaVersion">The current beta Stardew Valley version.</param>
        /// <param name="mods">The mod data.</param>
        /// <param name="cachedMetadata">The stored metadata record.</param>
        /// <param name="cachedMods">The stored mod records.</param>
        public void SaveWikiData(string stableVersion, string betaVersion, IEnumerable<WikiModEntry> mods, out CachedWikiMetadata cachedMetadata, out CachedWikiMod[] cachedMods)
        {
            cachedMetadata = new CachedWikiMetadata(stableVersion, betaVersion);
            cachedMods = mods.Select(mod => new CachedWikiMod(mod)).ToArray();

            this.WikiMods.DeleteMany("{}");
            this.WikiMods.InsertMany(cachedMods);

            this.WikiMetadata.DeleteMany("{}");
            this.WikiMetadata.InsertOne(cachedMetadata);
        }
    }
}
