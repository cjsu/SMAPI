using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.GitHub
{
    /// <summary>Basic metadata about a GitHub project.</summary>
    internal class GitRepo
    {
        /// <summary>The full repository name, including the owner.</summary>
        [JsonProperty("full_name")]
        public string FullName { get; set; }

        /// <summary>The URL to the repository web page, if any.</summary>
        [JsonProperty("html_url")]
        public string WebUrl { get; set; }

        /// <summary>The code license, if any.</summary>
        [JsonProperty("license")]
        public GitLicense License { get; set; }
    }
}
