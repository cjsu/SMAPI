namespace StardewModdingAPI.Web.Framework.LogParsing.Models
{
    /// <summary>Metadata about a mod or content pack in the log.</summary>
    public class LogModInfo
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod name.</summary>
        public string Name { get; set; }

        /// <summary>The mod author.</summary>
        public string Author { get; set; }

        /// <summary>The update version.</summary>
        public string UpdateVersion { get; set; }

        /// <summary>The update link.</summary>
        public string UpdateLink { get; set; }

        /// <summary>The mod version.</summary>
        public string Version { get; set; }

        /// <summary>The mod description.</summary>
        public string Description { get; set; }

        /// <summary>The name of the mod for which this is a content pack (if applicable).</summary>
        public string ContentPackFor { get; set; }

        /// <summary>The number of errors logged by this mod.</summary>
        public int Errors { get; set; }

        /// <summary>Whether the mod was loaded into the game.</summary>
        public bool Loaded { get; set; }

        /// <summary>Whether the mod has an update available.</summary>
        public bool HasUpdate => this.UpdateVersion != null && this.Version != this.UpdateVersion;
    }
}
