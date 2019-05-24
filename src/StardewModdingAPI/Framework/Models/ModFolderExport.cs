namespace StardewModdingAPI.Framework.Models
{
    /// <summary>Metadata exported to the mod folder.</summary>
    internal class ModFolderExport
    {
        /// <summary>When the export was generated.</summary>
        public string Exported { get; set; }

        /// <summary>The absolute path of the mod folder.</summary>
        public string ModFolderPath { get; set; }

        /// <summary>The game version which last loaded the mods.</summary>
        public string GameVersion { get; set; }

        /// <summary>The SMAPI version which last loaded the mods.</summary>
        public string ApiVersion { get; set; }

        /// <summary>The detected mods.</summary>
        public IModMetadata[] Mods { get; set; }
    }
}
