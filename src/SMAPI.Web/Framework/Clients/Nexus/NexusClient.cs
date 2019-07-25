using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Pathoschild.FluentNexus.Models;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit;
using FluentNexusClient = Pathoschild.FluentNexus.NexusClient;

namespace StardewModdingAPI.Web.Framework.Clients.Nexus
{
    /// <summary>An HTTP client for fetching mod metadata from the Nexus website.</summary>
    internal class NexusClient : INexusClient
    {
        /*********
        ** Fields
        *********/
        /// <summary>The URL for a Nexus mod page for the user, excluding the base URL, where {0} is the mod ID.</summary>
        private readonly string WebModUrlFormat;

        /// <summary>The URL for a Nexus mod page to scrape for versions, excluding the base URL, where {0} is the mod ID.</summary>
        public string WebModScrapeUrlFormat { get; set; }

        /// <summary>The underlying HTTP client for the Nexus Mods website.</summary>
        private readonly IClient WebClient;

        /// <summary>The underlying HTTP client for the Nexus API.</summary>
        private readonly FluentNexusClient ApiClient;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="webUserAgent">The user agent for the Nexus Mods web client.</param>
        /// <param name="webBaseUrl">The base URL for the Nexus Mods site.</param>
        /// <param name="webModUrlFormat">The URL for a Nexus Mods mod page for the user, excluding the <paramref name="webBaseUrl"/>, where {0} is the mod ID.</param>
        /// <param name="webModScrapeUrlFormat">The URL for a Nexus mod page to scrape for versions, excluding the base URL, where {0} is the mod ID.</param>
        /// <param name="apiAppVersion">The app version to show in API user agents.</param>
        /// <param name="apiKey">The Nexus API authentication key.</param>
        public NexusClient(string webUserAgent, string webBaseUrl, string webModUrlFormat, string webModScrapeUrlFormat, string apiAppVersion, string apiKey)
        {
            this.WebModUrlFormat = webModUrlFormat;
            this.WebModScrapeUrlFormat = webModScrapeUrlFormat;
            this.WebClient = new FluentClient(webBaseUrl).SetUserAgent(webUserAgent);
            this.ApiClient = new FluentNexusClient(apiKey, "SMAPI", apiAppVersion);
        }

        /// <summary>Get metadata about a mod.</summary>
        /// <param name="id">The Nexus mod ID.</param>
        /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
        public async Task<NexusMod> GetModAsync(uint id)
        {
            // Fetch from the Nexus website when possible, since it has no rate limits. Mods with
            // adult content are hidden for anonymous users, so fall back to the API in that case.
            // Note that the API has very restrictive rate limits which means we can't just use it
            // for all cases.
            NexusMod mod = await this.GetModFromWebsiteAsync(id);
            if (mod?.Status == NexusModStatus.AdultContentForbidden)
                mod = await this.GetModFromApiAsync(id);

            return mod;
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            this.WebClient?.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get metadata about a mod by scraping the Nexus website.</summary>
        /// <param name="id">The Nexus mod ID.</param>
        /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
        private async Task<NexusMod> GetModFromWebsiteAsync(uint id)
        {
            // fetch HTML
            string html;
            try
            {
                html = await this.WebClient
                    .GetAsync(string.Format(this.WebModScrapeUrlFormat, id))
                    .AsString();
            }
            catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound)
            {
                return null;
            }

            // parse HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // handle Nexus error message
            HtmlNode node = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'site-notice')][contains(@class, 'warning')]");
            if (node != null)
            {
                string[] errorParts = node.InnerText.Trim().Split(new[] { '\n' }, 2, System.StringSplitOptions.RemoveEmptyEntries);
                string errorCode = errorParts[0];
                string errorText = errorParts.Length > 1 ? errorParts[1] : null;
                switch (errorCode.Trim().ToLower())
                {
                    case "not found":
                        return null;

                    default:
                        return new NexusMod { Error = $"Nexus error: {errorCode} ({errorText}).", Status = this.GetWebStatus(errorCode) };
                }
            }

            // extract mod info
            string url = this.GetModUrl(id);
            string name = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText.Trim();
            string version = doc.DocumentNode.SelectSingleNode("//ul[contains(@class, 'stats')]//li[@class='stat-version']//div[@class='stat']")?.InnerText.Trim();
            SemanticVersion.TryParse(version, out ISemanticVersion parsedVersion);

            // extract file versions
            List<string> rawVersions = new List<string>();
            foreach (var fileSection in doc.DocumentNode.SelectNodes("//div[contains(@class, 'files-tabs')]"))
            {
                string sectionName = fileSection.Descendants("h2").First().InnerText;
                if (sectionName != "Main files" && sectionName != "Optional files")
                    continue;

                rawVersions.AddRange(
                    from statBox in fileSection.Descendants().Where(p => p.HasClass("stat-version"))
                    from versionStat in statBox.Descendants().Where(p => p.HasClass("stat"))
                    select versionStat.InnerText.Trim()
                );
            }

            // choose latest file version
            ISemanticVersion latestFileVersion = null;
            foreach (string rawVersion in rawVersions)
            {
                if (!SemanticVersion.TryParse(rawVersion, out ISemanticVersion cur))
                    continue;
                if (parsedVersion != null && !cur.IsNewerThan(parsedVersion))
                    continue;
                if (latestFileVersion != null && !cur.IsNewerThan(latestFileVersion))
                    continue;

                latestFileVersion = cur;
            }

            // yield info
            return new NexusMod
            {
                Name = name,
                Version = parsedVersion?.ToString() ?? version,
                LatestFileVersion = latestFileVersion,
                Url = url
            };
        }

        /// <summary>Get metadata about a mod from the Nexus API.</summary>
        /// <param name="id">The Nexus mod ID.</param>
        /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
        private async Task<NexusMod> GetModFromApiAsync(uint id)
        {
            // fetch mod
            Mod mod = await this.ApiClient.Mods.GetMod("stardewvalley", (int)id);
            ModFileList files = await this.ApiClient.ModFiles.GetModFiles("stardewvalley", (int)id, FileCategory.Main, FileCategory.Optional);

            // get versions
            if (!SemanticVersion.TryParse(mod.Version, out ISemanticVersion mainVersion))
                mainVersion = null;
            ISemanticVersion latestFileVersion = null;
            foreach (string rawVersion in files.Files.Select(p => p.FileVersion))
            {
                if (!SemanticVersion.TryParse(rawVersion, out ISemanticVersion cur))
                    continue;
                if (mainVersion != null && !cur.IsNewerThan(mainVersion))
                    continue;
                if (latestFileVersion != null && !cur.IsNewerThan(latestFileVersion))
                    continue;

                latestFileVersion = cur;
            }

            // yield info
            return new NexusMod
            {
                Name = mod.Name,
                Version = SemanticVersion.TryParse(mod.Version, out ISemanticVersion version) ? version?.ToString() : mod.Version,
                LatestFileVersion = latestFileVersion,
                Url = this.GetModUrl(id)
            };
        }

        /// <summary>Get the full mod page URL for a given ID.</summary>
        /// <param name="id">The mod ID.</param>
        private string GetModUrl(uint id)
        {
            UriBuilder builder = new UriBuilder(this.WebClient.BaseClient.BaseAddress);
            builder.Path += string.Format(this.WebModUrlFormat, id);
            return builder.Uri.ToString();
        }

        /// <summary>Get the mod status for a web error code.</summary>
        /// <param name="errorCode">The Nexus error code.</param>
        private NexusModStatus GetWebStatus(string errorCode)
        {
            switch (errorCode.Trim().ToLower())
            {
                case "adult content":
                    return NexusModStatus.AdultContentForbidden;

                case "hidden mod":
                    return NexusModStatus.Hidden;

                case "not published":
                    return NexusModStatus.NotPublished;

                default:
                    return NexusModStatus.Other;
            }
        }
    }
}
