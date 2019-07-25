using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit;

namespace StardewModdingAPI.Web.Framework.Clients.Nexus
{
    /// <summary>An HTTP client for fetching mod metadata from the Nexus website.</summary>
    internal class NexusWebScrapeClient : INexusClient
    {
        /*********
        ** Fields
        *********/
        /// <summary>The URL for a Nexus mod page for the user, excluding the base URL, where {0} is the mod ID.</summary>
        private readonly string ModUrlFormat;

        /// <summary>The URL for a Nexus mod page to scrape for versions, excluding the base URL, where {0} is the mod ID.</summary>
        public string ModScrapeUrlFormat { get; set; }

        /// <summary>The underlying HTTP client.</summary>
        private readonly IClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="userAgent">The user agent for the Nexus Mods API client.</param>
        /// <param name="baseUrl">The base URL for the Nexus Mods site.</param>
        /// <param name="modUrlFormat">The URL for a Nexus Mods mod page for the user, excluding the <paramref name="baseUrl"/>, where {0} is the mod ID.</param>
        /// <param name="modScrapeUrlFormat">The URL for a Nexus mod page to scrape for versions, excluding the base URL, where {0} is the mod ID.</param>
        public NexusWebScrapeClient(string userAgent, string baseUrl, string modUrlFormat, string modScrapeUrlFormat)
        {
            this.ModUrlFormat = modUrlFormat;
            this.ModScrapeUrlFormat = modScrapeUrlFormat;
            this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
        }

        /// <summary>Get metadata about a mod.</summary>
        /// <param name="id">The Nexus mod ID.</param>
        /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
        public async Task<NexusMod> GetModAsync(uint id)
        {
            // fetch HTML
            string html;
            try
            {
                html = await this.Client
                    .GetAsync(string.Format(this.ModScrapeUrlFormat, id))
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

                    case "hidden mod":
                        return new NexusMod { Error = $"Nexus error: {errorCode} ({errorText}).", Status = NexusModStatus.Hidden };

                    case "not published":
                        return new NexusMod { Error = $"Nexus error: {errorCode} ({errorText}).", Status = NexusModStatus.NotPublished };

                    default:
                        return new NexusMod { Error = $"Nexus error: {errorCode} ({errorText}).", Status = NexusModStatus.Other };
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

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            this.Client?.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the full mod page URL for a given ID.</summary>
        /// <param name="id">The mod ID.</param>
        private string GetModUrl(uint id)
        {
            UriBuilder builder = new UriBuilder(this.Client.BaseClient.BaseAddress);
            builder.Path += string.Format(this.ModUrlFormat, id);
            return builder.Uri.ToString();
        }
    }
}
