using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Web.Framework.Caching.Mods;
using StardewModdingAPI.Web.Framework.Caching.Wiki;
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

        /// <summary>The cache in which to store wiki data.</summary>
        private readonly IWikiCacheRepository WikiCache;

        /// <summary>The cache in which to store mod data.</summary>
        private readonly IModCacheRepository ModCache;

        /// <summary>The number of minutes successful update checks should be cached before refetching them.</summary>
        private readonly int SuccessCacheMinutes;

        /// <summary>The number of minutes failed update checks should be cached before refetching them.</summary>
        private readonly int ErrorCacheMinutes;

        /// <summary>The internal mod metadata list.</summary>
        private readonly ModDatabase ModDatabase;

        /// <summary>The web URL for the compatibility list.</summary>
        private readonly string CompatibilityPageUrl;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="environment">The web hosting environment.</param>
        /// <param name="wikiCache">The cache in which to store wiki data.</param>
        /// <param name="modCache">The cache in which to store mod metadata.</param>
        /// <param name="configProvider">The config settings for mod update checks.</param>
        /// <param name="chucklefish">The Chucklefish API client.</param>
        /// <param name="github">The GitHub API client.</param>
        /// <param name="modDrop">The ModDrop API client.</param>
        /// <param name="nexus">The Nexus API client.</param>
        public ModsApiController(IHostingEnvironment environment, IWikiCacheRepository wikiCache, IModCacheRepository modCache, IOptions<ModUpdateCheckConfig> configProvider, IChucklefishClient chucklefish, IGitHubClient github, IModDropClient modDrop, INexusClient nexus)
        {
            this.ModDatabase = new ModToolkit().GetModDatabase(Path.Combine(environment.WebRootPath, "SMAPI.metadata.json"));
            ModUpdateCheckConfig config = configProvider.Value;
            this.CompatibilityPageUrl = config.CompatibilityPageUrl;

            this.WikiCache = wikiCache;
            this.ModCache = modCache;
            this.SuccessCacheMinutes = config.SuccessCacheMinutes;
            this.ErrorCacheMinutes = config.ErrorCacheMinutes;
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
            WikiModEntry[] wikiData = this.WikiCache.GetWikiMods().Select(p => p.GetModel()).ToArray();
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
            // cross-reference data
            ModDataRecord record = this.ModDatabase.Get(search.ID);
            WikiModEntry wikiEntry = wikiData.FirstOrDefault(entry => entry.ID.Contains(search.ID.Trim(), StringComparer.InvariantCultureIgnoreCase));
            UpdateKey[] updateKeys = this.GetUpdateKeys(search.UpdateKeys, record, wikiEntry).ToArray();

            // get latest versions
            ModEntryModel result = new ModEntryModel { ID = search.ID };
            IList<string> errors = new List<string>();
            foreach (UpdateKey updateKey in updateKeys)
            {
                // validate update key
                if (!updateKey.LooksValid)
                {
                    errors.Add($"The update key '{updateKey}' isn't in a valid format. It should contain the site key and mod ID like 'Nexus:541'.");
                    continue;
                }

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

        /// <summary>Get the mod info for an update key.</summary>
        /// <param name="updateKey">The namespaced update key.</param>
        private async Task<ModInfoModel> GetInfoForUpdateKeyAsync(UpdateKey updateKey)
        {
            // get mod
            if (!this.ModCache.TryGetMod(updateKey.Repository, updateKey.ID, out CachedMod mod) || this.ModCache.IsStale(mod.LastUpdated, mod.FetchStatus == RemoteModStatus.TemporaryError ? this.ErrorCacheMinutes : this.SuccessCacheMinutes))
            {
                // get site
                if (!this.Repositories.TryGetValue(updateKey.Repository, out IModRepository repository))
                    return new ModInfoModel().SetError(RemoteModStatus.DoesNotExist, $"There's no mod site with key '{updateKey.Repository}'. Expected one of [{string.Join(", ", this.Repositories.Keys)}].");

                // fetch mod
                ModInfoModel result = await repository.GetModInfoAsync(updateKey.ID);
                if (result.Error == null)
                {
                    if (result.Version == null)
                        result.SetError(RemoteModStatus.InvalidData, $"The update key '{updateKey}' matches a mod with no version number.");
                    else if (!SemanticVersion.TryParse(result.Version, out _))
                        result.SetError(RemoteModStatus.InvalidData, $"The update key '{updateKey}' matches a mod with invalid semantic version '{result.Version}'.");
                }

                // cache mod
                this.ModCache.SaveMod(repository.VendorKey, updateKey.ID, result, out mod);
            }
            return mod.GetModel();
        }

        /// <summary>Get update keys based on the available mod metadata, while maintaining the precedence order.</summary>
        /// <param name="specifiedKeys">The specified update keys.</param>
        /// <param name="record">The mod's entry in SMAPI's internal database.</param>
        /// <param name="entry">The mod's entry in the wiki list.</param>
        public IEnumerable<UpdateKey> GetUpdateKeys(string[] specifiedKeys, ModDataRecord record, WikiModEntry entry)
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

            HashSet<UpdateKey> seen = new HashSet<UpdateKey>();
            foreach (string rawKey in GetRaw())
            {
                if (string.IsNullOrWhiteSpace(rawKey))
                    continue;

                UpdateKey key = UpdateKey.Parse(rawKey);
                if (seen.Add(key))
                    yield return key;
            }
        }
    }
}
