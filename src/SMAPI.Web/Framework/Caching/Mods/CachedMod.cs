using System;
using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.ModRepositories;

namespace StardewModdingAPI.Web.Framework.Caching.Mods
{
    /// <summary>The model for cached mod data.</summary>
    internal class CachedMod
    {
        /*********
        ** Accessors
        *********/
        /****
        ** Tracking
        ****/
        /// <summary>The internal MongoDB ID.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named per MongoDB conventions.")]
        [BsonIgnoreIfDefault]
        public ObjectId _id { get; set; }

        /// <summary>When the data was last updated.</summary>
        public DateTimeOffset LastUpdated { get; set; }

        /// <summary>When the data was last requested through the web API.</summary>
        public DateTimeOffset LastRequested { get; set; }

        /****
        ** Metadata
        ****/
        /// <summary>The mod site on which the mod is found.</summary>
        public ModRepositoryKey Site { get; set; }

        /// <summary>The mod's unique ID within the <see cref="Site"/>.</summary>
        public string ID { get; set; }

        /// <summary>The mod availability status on the remote site.</summary>
        public RemoteModStatus FetchStatus { get; set; }

        /// <summary>The error message providing more info for the <see cref="FetchStatus"/>, if applicable.</summary>
        public string FetchError { get; set; }


        /****
        ** Mod info
        ****/
        /// <summary>The mod's display name.</summary>
        public string Name { get; set; }

        /// <summary>The mod's latest version.</summary>
        public string MainVersion { get; set; }

        /// <summary>The mod's latest optional or prerelease version, if newer than <see cref="MainVersion"/>.</summary>
        public string PreviewVersion { get; set; }

        /// <summary>The URL for the mod page.</summary>
        public string Url { get; set; }


        /*********
        ** Accessors
        *********/
        /// <summary>Construct an instance.</summary>
        public CachedMod() { }

        /// <summary>Construct an instance.</summary>
        /// <param name="site">The mod site on which the mod is found.</param>
        /// <param name="id">The mod's unique ID within the <paramref name="site"/>.</param>
        /// <param name="mod">The mod data.</param>
        public CachedMod(ModRepositoryKey site, string id, ModInfoModel mod)
        {
            // tracking
            this.LastUpdated = DateTimeOffset.UtcNow;
            this.LastRequested = DateTimeOffset.UtcNow;

            // metadata
            this.Site = site;
            this.ID = id;
            this.FetchStatus = mod.Status;
            this.FetchError = mod.Error;

            // mod info
            this.Name = mod.Name;
            this.MainVersion = mod.Version;
            this.PreviewVersion = mod.PreviewVersion;
            this.Url = mod.Url;
        }

        /// <summary>Get the API model for the cached data.</summary>
        public ModInfoModel GetModel()
        {
            return new ModInfoModel(name: this.Name, version: this.MainVersion, previewVersion: this.PreviewVersion, url: this.Url).WithError(this.FetchStatus, this.FetchError);
        }
    }
}
