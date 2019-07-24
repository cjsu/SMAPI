using System;
using System.Diagnostics.CodeAnalysis;
using MongoDB.Bson;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

namespace StardewModdingAPI.Web.Framework.Caching.Wiki
{
    /// <summary>The model for cached wiki mods.</summary>
    internal class CachedWikiMod
    {
        /*********
        ** Accessors
        *********/
        /****
        ** Tracking
        ****/
        /// <summary>The internal MongoDB ID.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named per MongoDB conventions.")]
        public ObjectId _id { get; set; }

        /// <summary>When the data was last updated.</summary>
        public DateTimeOffset LastUpdated { get; set; }

        /****
        ** Mod info
        ****/
        /// <summary>The mod's unique ID. If the mod has alternate/old IDs, they're listed in latest to newest order.</summary>
        public string[] ID { get; set; }

        /// <summary>The mod's display name. If the mod has multiple names, the first one is the most canonical name.</summary>
        public string[] Name { get; set; }

        /// <summary>The mod's author name.  If the author has multiple names, the first one is the most canonical name.</summary>
        public string[] Author { get; set; }

        /// <summary>The mod ID on Nexus.</summary>
        public int? NexusID { get; set; }

        /// <summary>The mod ID in the Chucklefish mod repo.</summary>
        public int? ChucklefishID { get; set; }

        /// <summary>The mod ID in the ModDrop mod repo.</summary>
        public int? ModDropID { get; set; }

        /// <summary>The GitHub repository in the form 'owner/repo'.</summary>
        public string GitHubRepo { get; set; }

        /// <summary>The URL to a non-GitHub source repo.</summary>
        public string CustomSourceUrl { get; set; }

        /// <summary>The custom mod page URL (if applicable).</summary>
        public string CustomUrl { get; set; }

        /// <summary>The name of the mod which loads this content pack, if applicable.</summary>
        public string ContentPackFor { get; set; }

        /// <summary>The human-readable warnings for players about this mod.</summary>
        public string[] Warnings { get; set; }

        /// <summary>Extra metadata links (usually for open pull requests).</summary>
        public Tuple<Uri, string>[] MetadataLinks { get; set; }

        /// <summary>Special notes intended for developers who maintain unofficial updates or submit pull requests. </summary>
        public string DevNote { get; set; }

        /// <summary>The link anchor for the mod entry in the wiki compatibility list.</summary>
        public string Anchor { get; set; }

        /****
        ** Stable compatibility
        ****/
        /// <summary>The compatibility status.</summary>
        public WikiCompatibilityStatus MainStatus { get; set; }

        /// <summary>The human-readable summary of the compatibility status or workaround, without HTML formatting.</summary>
        public string MainSummary { get; set; }

        /// <summary>The game or SMAPI version which broke this mod (if applicable).</summary>
        public string MainBrokeIn { get; set; }

        /// <summary>The version of the latest unofficial update, if applicable.</summary>
        public string MainUnofficialVersion { get; set; }

        /// <summary>The URL to the latest unofficial update, if applicable.</summary>
        public string MainUnofficialUrl { get; set; }

        /****
        ** Beta compatibility
        ****/
        /// <summary>The compatibility status.</summary>
        public WikiCompatibilityStatus? BetaStatus { get; set; }

        /// <summary>The human-readable summary of the compatibility status or workaround, without HTML formatting.</summary>
        public string BetaSummary { get; set; }

        /// <summary>The game or SMAPI version which broke this mod (if applicable).</summary>
        public string BetaBrokeIn { get; set; }

        /// <summary>The version of the latest unofficial update, if applicable.</summary>
        public string BetaUnofficialVersion { get; set; }

        /// <summary>The URL to the latest unofficial update, if applicable.</summary>
        public string BetaUnofficialUrl { get; set; }


        /*********
        ** Accessors
        *********/
        /// <summary>Construct an instance.</summary>
        public CachedWikiMod() { }

        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod data.</param>
        public CachedWikiMod(WikiModEntry mod)
        {
            // tracking
            this.LastUpdated = DateTimeOffset.UtcNow;

            // mod info
            this.ID = mod.ID;
            this.Name = mod.Name;
            this.Author = mod.Author;
            this.NexusID = mod.NexusID;
            this.ChucklefishID = mod.ChucklefishID;
            this.ModDropID = mod.ModDropID;
            this.GitHubRepo = mod.GitHubRepo;
            this.CustomSourceUrl = mod.CustomSourceUrl;
            this.CustomUrl = mod.CustomUrl;
            this.ContentPackFor = mod.ContentPackFor;
            this.MetadataLinks = mod.MetadataLinks;
            this.Warnings = mod.Warnings;
            this.DevNote = mod.DevNote;
            this.Anchor = mod.Anchor;

            // stable compatibility
            this.MainStatus = mod.Compatibility.Status;
            this.MainSummary = mod.Compatibility.Summary;
            this.MainBrokeIn = mod.Compatibility.BrokeIn;
            this.MainUnofficialVersion = mod.Compatibility.UnofficialVersion?.ToString();
            this.MainUnofficialUrl = mod.Compatibility.UnofficialUrl;

            // beta compatibility
            this.BetaStatus = mod.BetaCompatibility?.Status;
            this.BetaSummary = mod.BetaCompatibility?.Summary;
            this.BetaBrokeIn = mod.BetaCompatibility?.BrokeIn;
            this.BetaUnofficialVersion = mod.BetaCompatibility?.UnofficialVersion?.ToString();
            this.BetaUnofficialUrl = mod.BetaCompatibility?.UnofficialUrl;
        }

        /// <summary>Reconstruct the original model.</summary>
        public WikiModEntry GetModel()
        {
            var mod = new WikiModEntry
            {
                ID = this.ID,
                Name = this.Name,
                Author = this.Author,
                NexusID = this.NexusID,
                ChucklefishID = this.ChucklefishID,
                ModDropID = this.ModDropID,
                GitHubRepo = this.GitHubRepo,
                CustomSourceUrl = this.CustomSourceUrl,
                CustomUrl = this.CustomUrl,
                ContentPackFor = this.ContentPackFor,
                Warnings = this.Warnings,
                MetadataLinks = this.MetadataLinks,
                DevNote = this.DevNote,
                Anchor = this.Anchor,

                // stable compatibility
                Compatibility = new WikiCompatibilityInfo
                {
                    Status = this.MainStatus,
                    Summary = this.MainSummary,
                    BrokeIn = this.MainBrokeIn,
                    UnofficialVersion = this.MainUnofficialVersion != null ? new SemanticVersion(this.MainUnofficialVersion) : null,
                    UnofficialUrl = this.MainUnofficialUrl
                }
            };

            // beta compatibility
            if (this.BetaStatus != null)
            {
                mod.BetaCompatibility = new WikiCompatibilityInfo
                {
                    Status = this.BetaStatus.Value,
                    Summary = this.BetaSummary,
                    BrokeIn = this.BetaBrokeIn,
                    UnofficialVersion = this.BetaUnofficialVersion != null ? new SemanticVersion(this.BetaUnofficialVersion) : null,
                    UnofficialUrl = this.BetaUnofficialUrl
                };
            }

            return mod;
        }
    }
}
