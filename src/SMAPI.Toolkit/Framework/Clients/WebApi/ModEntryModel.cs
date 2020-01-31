using System;

namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi
{
    /// <summary>Metadata about a mod.</summary>
    public class ModEntryModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod's unique ID (if known).</summary>
        public string ID { get; set; }

        /// <summary>The update version recommended by the web API based on its version update and mapping rules.</summary>
        public ModEntryVersionModel SuggestedUpdate { get; set; }

        /// <summary>Optional extended data which isn't needed for update checks.</summary>
        public ModExtendedMetadataModel Metadata { get; set; }

        /// <summary>The main version.</summary>
        [Obsolete]
        public ModEntryVersionModel Main { get; set; }

        /// <summary>The latest optional version, if newer than <see cref="Main"/>.</summary>
        [Obsolete]
        public ModEntryVersionModel Optional { get; set; }

        /// <summary>The latest unofficial version, if newer than <see cref="Main"/> and <see cref="Optional"/>.</summary>
        [Obsolete]
        public ModEntryVersionModel Unofficial { get; set; }

        /// <summary>The latest unofficial version for the current Stardew Valley or SMAPI beta, if any (see <see cref="HasBetaInfo"/>).</summary>
        [Obsolete]
        public ModEntryVersionModel UnofficialForBeta { get; set; }

        /// <summary>Whether a Stardew Valley or SMAPI beta which affects mod compatibility is in progress. If this is true, <see cref="UnofficialForBeta"/> should be used for beta versions of SMAPI instead of <see cref="Unofficial"/>.</summary>
        [Obsolete]
        public bool? HasBetaInfo { get; set; }

        /// <summary>The errors that occurred while fetching update data.</summary>
        public string[] Errors { get; set; } = new string[0];
    }
}
