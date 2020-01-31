using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki
{
    /// <summary>A mod entry in the wiki list.</summary>
    public class WikiModEntry
    {
        /*********
        ** Accessors
        *********/
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

        /// <summary>The mod ID in the CurseForge mod repo.</summary>
        public int? CurseForgeID { get; set; }

        /// <summary>The mod key in the CurseForge mod repo (used in mod page URLs).</summary>
        public string CurseForgeKey { get; set; }

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

        /// <summary>The mod's compatibility with the latest stable version of the game.</summary>
        public WikiCompatibilityInfo Compatibility { get; set; }

        /// <summary>The mod's compatibility with the latest beta version of the game (if any).</summary>
        public WikiCompatibilityInfo BetaCompatibility { get; set; }

        /// <summary>Whether a Stardew Valley or SMAPI beta which affects mod compatibility is in progress. If this is true, <see cref="BetaCompatibility"/> should be used for beta versions of SMAPI instead of <see cref="Compatibility"/>.</summary>
        public bool HasBetaInfo => this.BetaCompatibility != null;

        /// <summary>The human-readable warnings for players about this mod.</summary>
        public string[] Warnings { get; set; }

        /// <summary>Extra metadata links (usually for open pull requests).</summary>
        public Tuple<Uri, string>[] MetadataLinks { get; set; }

        /// <summary>Special notes intended for developers who maintain unofficial updates or submit pull requests. </summary>
        public string DevNote { get; set; }

        /// <summary>Maps local versions to a semantic version for update checks.</summary>
        public IDictionary<string, string> MapLocalVersions { get; set; }

        /// <summary>Maps remote versions to a semantic version for update checks.</summary>
        public IDictionary<string, string> MapRemoteVersions { get; set; }

        /// <summary>The link anchor for the mod entry in the wiki compatibility list.</summary>
        public string Anchor { get; set; }
    }
}
