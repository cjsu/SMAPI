using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Pathoschild.Http.Client;

namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki
{
    /// <summary>An HTTP client for fetching mod metadata from the wiki.</summary>
    public class WikiClient : IDisposable
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying HTTP client.</summary>
        private readonly IClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="userAgent">The user agent for the wiki API.</param>
        /// <param name="baseUrl">The base URL for the wiki API.</param>
        public WikiClient(string userAgent, string baseUrl = "https://stardewvalleywiki.com/mediawiki/api.php")
        {
            this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
        }

        /// <summary>Fetch mods from the compatibility list.</summary>
        public async Task<WikiModList> FetchModsAsync()
        {
            // fetch HTML
            ResponseModel response = await this.Client
                .GetAsync("")
                .WithArguments(new
                {
                    action = "parse",
                    page = "Modding:Mod_compatibility",
                    format = "json"
                })
                .As<ResponseModel>();
            string html = response.Parse.Text["*"];

            // parse HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // fetch game versions
            string stableVersion = doc.DocumentNode.SelectSingleNode("div[@class='game-stable-version']")?.InnerText;
            string betaVersion = doc.DocumentNode.SelectSingleNode("div[@class='game-beta-version']")?.InnerText;
            if (betaVersion == stableVersion)
                betaVersion = null;

            // find mod entries
            HtmlNodeCollection modNodes = doc.DocumentNode.SelectNodes("table[@id='mod-list']//tr[@class='mod']");
            if (modNodes == null)
                throw new InvalidOperationException("Can't parse wiki compatibility list, no mods found.");

            // parse
            WikiModEntry[] mods = this.ParseEntries(modNodes).ToArray();
            return new WikiModList
            {
                StableVersion = stableVersion,
                BetaVersion = betaVersion,
                Mods = mods
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
        /// <summary>Parse valid mod compatibility entries.</summary>
        /// <param name="nodes">The HTML compatibility entries.</param>
        private IEnumerable<WikiModEntry> ParseEntries(IEnumerable<HtmlNode> nodes)
        {
            foreach (HtmlNode node in nodes)
            {
                // extract fields
                string[] names = this.GetAttributeAsCsv(node, "data-name");
                string[] authors = this.GetAttributeAsCsv(node, "data-author");
                string[] ids = this.GetAttributeAsCsv(node, "data-id");
                string[] warnings = this.GetAttributeAsCsv(node, "data-warnings");
                int? nexusID = this.GetAttributeAsNullableInt(node, "data-nexus-id");
                int? chucklefishID = this.GetAttributeAsNullableInt(node, "data-cf-id");
                int? curseForgeID = this.GetAttributeAsNullableInt(node, "data-curseforge-id");
                string curseForgeKey = this.GetAttribute(node, "data-curseforge-key");
                int? modDropID = this.GetAttributeAsNullableInt(node, "data-moddrop-id");
                string githubRepo = this.GetAttribute(node, "data-github");
                string customSourceUrl = this.GetAttribute(node, "data-custom-source");
                string customUrl = this.GetAttribute(node, "data-url");
                string anchor = this.GetAttribute(node, "id");
                string contentPackFor = this.GetAttribute(node, "data-content-pack-for");
                string devNote = this.GetAttribute(node, "data-dev-note");
                IDictionary<string, string> mapLocalVersions = this.GetAttributeAsVersionMapping(node, "data-map-local-versions");
                IDictionary<string, string> mapRemoteVersions = this.GetAttributeAsVersionMapping(node, "data-map-remote-versions");

                // parse stable compatibility
                WikiCompatibilityInfo compatibility = new WikiCompatibilityInfo
                {
                    Status = this.GetAttributeAsEnum<WikiCompatibilityStatus>(node, "data-status") ?? WikiCompatibilityStatus.Ok,
                    BrokeIn = this.GetAttribute(node, "data-broke-in"),
                    UnofficialVersion = this.GetAttributeAsSemanticVersion(node, "data-unofficial-version"),
                    UnofficialUrl = this.GetAttribute(node, "data-unofficial-url"),
                    Summary = this.GetInnerHtml(node, "mod-summary")?.Trim()
                };

                // parse beta compatibility
                WikiCompatibilityInfo betaCompatibility = null;
                {
                    WikiCompatibilityStatus? betaStatus = this.GetAttributeAsEnum<WikiCompatibilityStatus>(node, "data-beta-status");
                    if (betaStatus.HasValue)
                    {
                        betaCompatibility = new WikiCompatibilityInfo
                        {
                            Status = betaStatus.Value,
                            BrokeIn = this.GetAttribute(node, "data-beta-broke-in"),
                            UnofficialVersion = this.GetAttributeAsSemanticVersion(node, "data-beta-unofficial-version"),
                            UnofficialUrl = this.GetAttribute(node, "data-beta-unofficial-url"),
                            Summary = this.GetInnerHtml(node, "mod-beta-summary")
                        };
                    }
                }

                // parse links
                List<Tuple<Uri, string>> metadataLinks = new List<Tuple<Uri, string>>();
                foreach (HtmlNode linkElement in node.Descendants("td").Last().Descendants("a").Skip(1)) // skip anchor link
                {
                    string text = linkElement.InnerText.Trim();
                    Uri url = new Uri(linkElement.GetAttributeValue("href", ""));
                    metadataLinks.Add(Tuple.Create(url, text));
                }

                // yield model
                yield return new WikiModEntry
                {
                    ID = ids,
                    Name = names,
                    Author = authors,
                    NexusID = nexusID,
                    ChucklefishID = chucklefishID,
                    CurseForgeID = curseForgeID,
                    CurseForgeKey = curseForgeKey,
                    ModDropID = modDropID,
                    GitHubRepo = githubRepo,
                    CustomSourceUrl = customSourceUrl,
                    CustomUrl = customUrl,
                    ContentPackFor = contentPackFor,
                    Compatibility = compatibility,
                    BetaCompatibility = betaCompatibility,
                    Warnings = warnings,
                    MetadataLinks = metadataLinks.ToArray(),
                    DevNote = devNote,
                    MapLocalVersions = mapLocalVersions,
                    MapRemoteVersions = mapRemoteVersions,
                    Anchor = anchor
                };
            }
        }

        /// <summary>Get an attribute value.</summary>
        /// <param name="element">The element whose attributes to read.</param>
        /// <param name="name">The attribute name.</param>
        private string GetAttribute(HtmlNode element, string name)
        {
            string value = element.GetAttributeValue(name, null);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return WebUtility.HtmlDecode(value);
        }

        /// <summary>Get an attribute value and parse it as a comma-delimited list of strings.</summary>
        /// <param name="element">The element whose attributes to read.</param>
        /// <param name="name">The attribute name.</param>
        private string[] GetAttributeAsCsv(HtmlNode element, string name)
        {
            string raw = this.GetAttribute(element, name);
            return !string.IsNullOrWhiteSpace(raw)
                ? raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray()
                : new string[0];
        }

        /// <summary>Get an attribute value and parse it as an enum value.</summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="element">The element whose attributes to read.</param>
        /// <param name="name">The attribute name.</param>
        private TEnum? GetAttributeAsEnum<TEnum>(HtmlNode element, string name) where TEnum : struct
        {
            string raw = this.GetAttribute(element, name);
            if (raw == null)
                return null;
            if (!Enum.TryParse(raw, true, out TEnum value) && Enum.IsDefined(typeof(TEnum), value))
                throw new InvalidOperationException($"Unknown {typeof(TEnum).Name} value '{raw}' when parsing compatibility list.");
            return value;
        }

        /// <summary>Get an attribute value and parse it as a semantic version.</summary>
        /// <param name="element">The element whose attributes to read.</param>
        /// <param name="name">The attribute name.</param>
        private ISemanticVersion GetAttributeAsSemanticVersion(HtmlNode element, string name)
        {
            string raw = this.GetAttribute(element, name);
            return SemanticVersion.TryParse(raw, out ISemanticVersion version)
                ? version
                : null;
        }

        /// <summary>Get an attribute value and parse it as a nullable int.</summary>
        /// <param name="element">The element whose attributes to read.</param>
        /// <param name="name">The attribute name.</param>
        private int? GetAttributeAsNullableInt(HtmlNode element, string name)
        {
            string raw = this.GetAttribute(element, name);
            if (raw != null && int.TryParse(raw, out int value))
                return value;
            return null;
        }

        /// <summary>Get an attribute value and parse it as a version mapping.</summary>
        /// <param name="element">The element whose attributes to read.</param>
        /// <param name="name">The attribute name.</param>
        private IDictionary<string, string> GetAttributeAsVersionMapping(HtmlNode element, string name)
        {
            // get raw value
            string raw = this.GetAttribute(element, name);
            if (raw?.Contains("→") != true)
                return null;

            // parse
            // Specified on the wiki in the form "remote version → mapped version; another remote version → mapped version"
            IDictionary<string, string> map = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (string pair in raw.Split(';'))
            {
                string[] versions = pair.Split('→');
                if (versions.Length == 2 && !string.IsNullOrWhiteSpace(versions[0]) && !string.IsNullOrWhiteSpace(versions[1]))
                    map[versions[0].Trim()] = versions[1].Trim();
            }
            return map;
        }

        /// <summary>Get the text of an element with the given class name.</summary>
        /// <param name="container">The metadata container.</param>
        /// <param name="className">The field name.</param>
        private string GetInnerHtml(HtmlNode container, string className)
        {
            return container.Descendants().FirstOrDefault(p => p.HasClass(className))?.InnerHtml;
        }

        /// <summary>The response model for the MediaWiki parse API.</summary>
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class ResponseModel
        {
            /// <summary>The parse API results.</summary>
            public ResponseParseModel Parse { get; set; }
        }

        /// <summary>The inner response model for the MediaWiki parse API.</summary>
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private class ResponseParseModel
        {
            /// <summary>The parsed text.</summary>
            public IDictionary<string, string> Text { get; set; }
        }
    }
}
