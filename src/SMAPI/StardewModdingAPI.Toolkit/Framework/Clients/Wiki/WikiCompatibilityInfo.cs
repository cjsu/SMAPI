namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki
{
    /// <summary>Compatibility info for a mod.</summary>
    public class WikiCompatibilityInfo
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The compatibility status.</summary>
        public WikiCompatibilityStatus Status { get; set; }

        /// <summary>The human-readable summary of the compatibility status or workaround, without HTML formatting.</summary>
        public string Summary { get; set; }

        /// <summary>The game or SMAPI version which broke this mod (if applicable).</summary>
        public string BrokeIn { get; set; }

        /// <summary>The version of the latest unofficial update, if applicable.</summary>
        public ISemanticVersion UnofficialVersion { get; set; }

        /// <summary>The URL to the latest unofficial update, if applicable.</summary>
        public string UnofficialUrl { get; set; }
    }
}
