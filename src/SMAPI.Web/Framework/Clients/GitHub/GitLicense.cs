using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub
{
    /// <summary>The license info for a GitHub project.</summary>
    internal class GitLicense
    {
        /// <summary>The license display name.</summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>The SPDX ID for the license.</summary>
        [JsonProperty("spdx_id")]
        public string SpdxId { get; set; }

        /// <summary>The URL for the license info.</summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
