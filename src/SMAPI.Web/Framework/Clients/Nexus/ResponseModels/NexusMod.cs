using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.Nexus.ResponseModels
{
    /// <summary>Mod metadata from Nexus Mods.</summary>
    internal class NexusMod
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod name.</summary>
        public string Name { get; set; }

        /// <summary>The mod's semantic version number.</summary>
        public string Version { get; set; }

        /// <summary>The mod's web URL.</summary>
        [JsonProperty("mod_page_uri")]
        public string Url { get; set; }

        /// <summary>The mod's publication status.</summary>
        [JsonIgnore]
        public NexusModStatus Status { get; set; } = NexusModStatus.Ok;

        /// <summary>The files available to download.</summary>
        [JsonIgnore]
        public IModDownload[] Downloads { get; set; }

        /// <summary>A custom user-friendly error which indicates why fetching the mod info failed (if applicable).</summary>
        [JsonIgnore]
        public string Error { get; set; }
    }
}
