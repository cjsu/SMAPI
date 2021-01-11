using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.ModDrop.ResponseModels
{
    /// <summary>Metadata from the ModDrop API about a mod file.</summary>
    public class FileDataModel
    {
        /// <summary>The file title.</summary>
        [JsonProperty("title")]
        public string Name { get; set; }

        /// <summary>The file description.</summary>
        [JsonProperty("desc")]
        public string Description { get; set; }

        /// <summary>The file version.</summary>
        public string Version { get; set; }

        /// <summary>Whether the file is deleted.</summary>
        public bool IsDeleted { get; set; }

        /// <summary>Whether the file is hidden from users.</summary>
        public bool IsHidden { get; set; }

        /// <summary>Whether this is the default file for the mod.</summary>
        public bool IsDefault { get; set; }

        /// <summary>Whether this is an archived file.</summary>
        public bool IsOld { get; set; }
    }
}
