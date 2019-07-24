using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Clients.Chucklefish;
using StardewModdingAPI.Web.Framework.Clients.GitHub;
using StardewModdingAPI.Web.Framework.Clients.ModDrop;
using StardewModdingAPI.Web.Framework.Clients.Nexus;
using StardewModdingAPI.Web.Framework.ConfigModels;
using StardewModdingAPI.Web.Framework.ModRepositories;

namespace StardewModdingAPI.Web.Controllers
{
    /// <summary>Provides an API to perform mod update checks.</summary>
    [Produces("application/json")]
    [Route("api/v{version:semanticVersion}/mods")]
    internal class ModsApiController : Controller
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod repositories which provide mod metadata.</summary>
        private readonly IDictionary<ModRepositoryKey, IModRepository> Repositories;

        /// <summary>The cache in which to store mod metadata.</summary>
        private readonly IMemoryCache Cache;

        /// <summary>The number of minutes successful update checks should be cached before refetching them.</summary>
        private readonly int SuccessCacheMinutes;

        /// <summary>The number of minutes failed update checks should be cached before refetching them.</summary>
        private readonly int ErrorCacheMinutes;

        /// <summary>A regex which matches SMAPI-style semantic version.</summary>
        private readonly string VersionRegex;

        /// <summary>The internal mod metadata list.</summary>
        private readonly ModDatabase ModDatabase;

        /// <summary>The web URL for the compatibility list.</summary>
        private readonly string CompatibilityPageUrl;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="environment">The web hosting environment.</param>
        /// <param name="cache">The cache in which to store mod metadata.</param>
        /// <param name="configProvider">The config settings for mod update checks.</param>
        /// <param name="chucklefish">The Chucklefish API client.</param>
        /// <param name="github">The GitHub API client.</param>
        /// <param name="modDrop">The ModDrop API client.</param>
        /// <param name="nexus">The Nexus API client.</param>
        public ModsApiController(IHostingEnvironment environment, IMemoryCache cache, IOptions<ModUpdateCheckConfig> configProvider, IChucklefishClient chucklefish, IGitHubClient github, IModDropClient modDrop, INexusClient nexus)
        {
            this.ModDatabase = new ModToolkit().GetModDatabase(Path.Combine(environment.WebRootPath, "SMAPI.metadata.json"));
            ModUpdateCheckConfig config = configProvider.Value;
            this.CompatibilityPageUrl = config.CompatibilityPageUrl;

            this.Cache = cache;
            this.SuccessCacheMinutes = config.SuccessCacheMinutes;
            this.ErrorCacheMinutes = config.ErrorCacheMinutes;
            this.VersionRegex = config.SemanticVersionRegex;
            this.Repositories =
                new IModRepository[]
                {
                    new ChucklefishRepository(chucklefish),
                    new GitHubRepository(github),
                    new ModDropRepository(modDrop),
                    new NexusRepository(nexus)
                }
                .ToDictionary(p => p.VendorKey);
        }

        /// <summary>Fetch version metadata for the given mods.</summary>
        /// <param name="model">The mod search criteria.</param>
        [HttpPost]
        public async Task<IEnumerable<ModEntryModel>> PostAsync([FromBody] ModSearchModel model)
        {
            if (model?.Mods == null)
                return new ModEntryModel[0];

            // fetch wiki data
            WikiModEntry[] wikiData = await this.GetWikiDataAsync();
            IDictionary<string, ModEntryModel> mods = new Dictionary<string, ModEntryModel>(StringComparer.CurrentCultureIgnoreCase);
            foreach (ModSearchEntryModel mod in model.Mods)
            {
                if (string.IsNullOrWhiteSpace(mod.ID))
                    continue;

                ModEntryModel result = await this.GetModData(mod, wikiData, model.IncludeExtendedMetadata);
                mods[mod.ID] = result;
            }

            // return data
            return mods.Values;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the metadata for a mod.</summary>
        /// <param name="search">The mod data to match.</param>
        /// <param name="wikiData">The wiki data.</param>
        /// <param name="includeExtendedMetadata">Whether to include extended metadata for each mod.</param>
        /// <returns>Returns the mod data if found, else <c>null</c>.</returns>
        private async Task<ModEntryModel> GetModData(ModSearchEntryModel search, WikiModEntry[] wikiData, bool includeExtendedMetadata)
        {
            // crossreference data
            ModDataRecord record = this.ModDatabase.Get(search.ID);
            WikiModEntry wikiEntry = wikiData.FirstOrDefault(entry => entry.ID.Contains(search.ID.Trim(), StringComparer.InvariantCultureIgnoreCase));
            string[] updateKeys = this.GetUpdateKeys(search.UpdateKeys, record, wikiEntry).ToArray();

            // get latest versions
            ModEntryModel result = new ModEntryModel { ID = search.ID };
            IList<string> errors = new List<string>();
            foreach (string updateKey in updateKeys)
            {
                // fetch data
                ModInfoModel data = await this.GetInfoForUpdateKeyAsync(updateKey);
                if (data.Error != null)
                {
                    errors.Add(data.Error);
                    continue;
                }

                // handle main version
                if (data.Version != null)
                {
                    if (!SemanticVersion.TryParse(data.Version, out ISemanticVersion version))
                    {
                        errors.Add($"The update key '{updateKey}' matches a mod with invalid semantic version '{data.Version}'.");
                        continue;
                    }

                    if (this.IsNewer(version, result.Main?.Version))
                        result.Main = new ModEntryVersionModel(version, data.Url);
                }

                // handle optional version
                if (data.PreviewVersion != null)
                {
                    if (!SemanticVersion.TryParse(data.PreviewVersion, out ISemanticVersion version))
                    {
                        errors.Add($"The update key '{updateKey}' matches a mod with invalid optional semantic version '{data.PreviewVersion}'.");
                        continue;
                    }

                    if (this.IsNewer(version, result.Optional?.Version))
                        result.Optional = new ModEntryVersionModel(version, data.Url);
                }
            }

            // get unofficial version
            if (wikiEntry?.Compatibility.UnofficialVersion != null && this.IsNewer(wikiEntry.Compatibility.UnofficialVersion, result.Main?.Version) && this.IsNewer(wikiEntry.Compatibility.UnofficialVersion, result.Optional?.Version))
                result.Unofficial = new ModEntryVersionModel(wikiEntry.Compatibility.UnofficialVersion, $"{this.CompatibilityPageUrl}/#{wikiEntry.Anchor}");

            // get unofficial version for beta
            if (wikiEntry?.HasBetaInfo == true)
            {
                result.HasBetaInfo = true;
                if (wikiEntry.BetaCompatibility.Status == WikiCompatibilityStatus.Unofficial)
                {
                    if (wikiEntry.BetaCompatibility.UnofficialVersion != null)
                    {
                        result.UnofficialForBeta = (wikiEntry.BetaCompatibility.UnofficialVersion != null && this.IsNewer(wikiEntry.BetaCompatibility.UnofficialVersion, result.Main?.Version) && this.IsNewer(wikiEntry.BetaCompatibility.UnofficialVersion, result.Optional?.Version))
                            ? new ModEntryVersionModel(wikiEntry.BetaCompatibility.UnofficialVersion, $"{this.CompatibilityPageUrl}/#{wikiEntry.Anchor}")
                            : null;
                    }
                    else
                        result.UnofficialForBeta = result.Unofficial;
                }
            }

            // fallback to preview if latest is invalid
            if (result.Main == null && result.Optional != null)
            {
                result.Main = result.Optional;
                result.Optional = null;
            }

            // special cases
            if (result.ID == "Pathoschild.SMAPI")
            {
                if (result.Main != null)
                    result.Main.Url = "https://smapi.io/";
                if (result.Optional != null)
                    result.Optional.Url = "https://smapi.io/";
            }

            // add extended metadata
            if (includeExtendedMetadata && (wikiEntry != null || record != null))
                result.Metadata = new ModExtendedMetadataModel(wikiEntry, record);

            // add result
            result.Errors = errors.ToArray();
            return result;
        }

        /// <summary>Get whether a <paramref name="current"/> version is newer than an <paramref name="other"/> version.</summary>
        /// <param name="current">The current version.</param>
        /// <param name="other">The other version.</param>
        private bool IsNewer(ISemanticVersion current, ISemanticVersion other)
        {
            return current != null && (other == null || other.IsOlderThan(current));
        }

        /// <summary>Get mod data from the wiki compatibility list.</summary>
        private async Task<WikiModEntry[]> GetWikiDataAsync()
        {
            ModToolkit toolkit = new ModToolkit();
            return await this.Cache.GetOrCreateAsync("_wiki", async entry =>
            {
                try
                {
                    WikiModEntry[] entries = (await toolkit.GetWikiCompatibilityListAsync()).Mods;
                    entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(this.SuccessCacheMinutes);
                    return entries;
                }
                catch
                {
                    entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(this.ErrorCacheMinutes);
                    return new WikiModEntry[0];
                }
            });
        }

        /// <summary>Get the mod info for an update key.</summary>
        /// <param name="updateKey">The namespaced update key.</param>
        private async Task<ModInfoModel> GetInfoForUpdateKeyAsync(string updateKey)
        {
            // parse update key
            UpdateKey parsed = UpdateKey.Parse(updateKey);
            if (!parsed.LooksValid)
                return new ModInfoModel().WithError(RemoteModStatus.DoesNotExist, $"The update key '{updateKey}' isn't in a valid format. It should contain the site key and mod ID like 'Nexus:541'.");

            // get matching repository
            if (!this.Repositories.TryGetValue(parsed.Repository, out IModRepository repository))
                return new ModInfoModel().WithError(RemoteModStatus.DoesNotExist, $"There's no mod site with key '{parsed.Repository}'. Expected one of [{string.Join(", ", this.Repositories.Keys)}].");

            // fetch mod info
            return await this.Cache.GetOrCreateAsync($"{repository.VendorKey}:{parsed.ID}".ToLower(), async entry =>
            {
                ModInfoModel result = await repository.GetModInfoAsync(parsed.ID);
                if (result.Error == null)
                {
                    if (result.Version == null)
                        result.WithError(RemoteModStatus.InvalidData, $"The update key '{updateKey}' matches a mod with no version number.");
                    else if (!Regex.IsMatch(result.Version, this.VersionRegex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                        result.WithError(RemoteModStatus.InvalidData, $"The update key '{updateKey}' matches a mod with invalid semantic version '{result.Version}'.");
                }
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(result.Status == RemoteModStatus.TemporaryError ? this.ErrorCacheMinutes : this.SuccessCacheMinutes);
                return result;
            });
        }

        /// <summary>Get update keys based on the available mod metadata, while maintaining the precedence order.</summary>
        /// <param name="specifiedKeys">The specified update keys.</param>
        /// <param name="record">The mod's entry in SMAPI's internal database.</param>
        /// <param name="entry">The mod's entry in the wiki list.</param>
        public IEnumerable<string> GetUpdateKeys(string[] specifiedKeys, ModDataRecord record, WikiModEntry entry)
        {
            IEnumerable<string> GetRaw()
            {
                // specified update keys
                if (specifiedKeys != null)
                {
                    foreach (string key in specifiedKeys)
                        yield return key?.Trim();
                }

                // default update key
                string defaultKey = record?.GetDefaultUpdateKey();
                if (defaultKey != null)
                    yield return defaultKey;

                // wiki metadata
                if (entry != null)
                {
                    if (entry.NexusID.HasValue)
                        yield return $"Nexus:{entry.NexusID}";
                    if (entry.ChucklefishID.HasValue)
                        yield return $"Chucklefish:{entry.ChucklefishID}";
                    if (entry.ModDropID.HasValue)
                        yield return $"ModDrop:{entry.ModDropID}";
                }
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.InvariantCulture);
            foreach (string key in GetRaw())
            {
                if (!string.IsNullOrWhiteSpace(key) && seen.Add(key))
                    yield return key;
            }
        }
    }
}
