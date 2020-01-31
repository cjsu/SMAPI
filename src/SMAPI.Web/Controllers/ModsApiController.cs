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
using StardewModdingAPI.Web.Framework;
using StardewModdingAPI.Web.Framework.Caching.Mods;
using StardewModdingAPI.Web.Framework.Caching.Wiki;
using StardewModdingAPI.Web.Framework.Clients.Chucklefish;
using StardewModdingAPI.Web.Framework.Clients.CurseForge;
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


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="environment">The web hosting environment.</param>
        /// <param name="wikiCache">The cache in which to store wiki data.</param>
        /// <param name="modCache">The cache in which to store mod metadata.</param>
        /// <param name="configProvider">The config settings for mod update checks.</param>
        /// <param name="chucklefish">The Chucklefish API client.</param>
        /// <param name="curseForge">The CurseForge API client.</param>
        /// <param name="github">The GitHub API client.</param>
        /// <param name="modDrop">The ModDrop API client.</param>
        /// <param name="nexus">The Nexus API client.</param>
        public ModsApiController(IHostingEnvironment environment, IWikiCacheRepository wikiCache, IModCacheRepository modCache, IOptions<ModUpdateCheckConfig> configProvider, IChucklefishClient chucklefish, ICurseForgeClient curseForge, IGitHubClient github, IModDropClient modDrop, INexusClient nexus)
        {
            this.ModDatabase = new ModToolkit().GetModDatabase(Path.Combine(environment.WebRootPath, "SMAPI.metadata.json"));
            ModUpdateCheckConfig config = configProvider.Value;

            this.WikiCache = wikiCache;
            this.ModCache = modCache;
            this.SuccessCacheMinutes = config.SuccessCacheMinutes;
            this.ErrorCacheMinutes = config.ErrorCacheMinutes;
            this.Repositories =
                new IModRepository[]
                {
                    new ChucklefishRepository(chucklefish),
                    new CurseForgeRepository(curseForge),
                    new GitHubRepository(github),
                    new ModDropRepository(modDrop),
                    new NexusRepository(nexus)
                }
                .ToDictionary(p => p.VendorKey);
        }

        /// <summary>Fetch version metadata for the given mods.</summary>
        /// <param name="model">The mod search criteria.</param>
        /// <param name="version">The requested API version.</param>
        [HttpPost]
        public async Task<IEnumerable<ModEntryModel>> PostAsync([FromBody] ModSearchModel model, [FromRoute] string version)
        {
            if (model?.Mods == null)
                return new ModEntryModel[0];

            bool legacyMode = SemanticVersion.TryParse(version, out ISemanticVersion parsedVersion) && parsedVersion.IsOlderThan("3.0.0-beta.20191109");

            // fetch wiki data
            WikiModEntry[] wikiData = this.WikiCache.GetWikiMods().Select(p => p.GetModel()).ToArray();
            IDictionary<string, ModEntryModel> mods = new Dictionary<string, ModEntryModel>(StringComparer.CurrentCultureIgnoreCase);
            foreach (ModSearchEntryModel mod in model.Mods)
            {
                if (string.IsNullOrWhiteSpace(mod.ID))
                    continue;

                ModEntryModel result = await this.GetModData(mod, wikiData, model.IncludeExtendedMetadata || legacyMode, model.ApiVersion);
                if (legacyMode)
                {
                    result.Main = result.Metadata.Main;
                    result.Optional = result.Metadata.Optional;
                    result.Unofficial = result.Metadata.Unofficial;
                    result.UnofficialForBeta = result.Metadata.UnofficialForBeta;
                    result.HasBetaInfo = result.Metadata.BetaCompatibilityStatus != null;
                    result.SuggestedUpdate = null;
                    if (!model.IncludeExtendedMetadata)
                        result.Metadata = null;
                }
                else if (!model.IncludeExtendedMetadata && (model.ApiVersion == null || mod.InstalledVersion == null))
                {
                    var errors = new List<string>(result.Errors);
                    errors.Add($"This API can't suggest an update because {nameof(model.ApiVersion)} or {nameof(mod.InstalledVersion)} are null, and you didn't specify {nameof(model.IncludeExtendedMetadata)} to get other info. See the SMAPI technical docs for usage.");
                    result.Errors = errors.ToArray();
                }

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
        /// <param name="apiVersion">The SMAPI version installed by the player.</param>
        /// <returns>Returns the mod data if found, else <c>null</c>.</returns>
        private async Task<ModEntryModel> GetModData(ModSearchEntryModel search, WikiModEntry[] wikiData, bool includeExtendedMetadata, ISemanticVersion apiVersion)
        {
            // cross-reference data
            ModDataRecord record = this.ModDatabase.Get(search.ID);
            WikiModEntry wikiEntry = wikiData.FirstOrDefault(entry => entry.ID.Contains(search.ID.Trim(), StringComparer.InvariantCultureIgnoreCase));
            UpdateKey[] updateKeys = this.GetUpdateKeys(search.UpdateKeys, record, wikiEntry).ToArray();

            // get latest versions
            ModEntryModel result = new ModEntryModel { ID = search.ID };
            IList<string> errors = new List<string>();
            ModEntryVersionModel main = null;
            ModEntryVersionModel optional = null;
            ModEntryVersionModel unofficial = null;
            ModEntryVersionModel unofficialForBeta = null;
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
                    ISemanticVersion version = this.GetMappedVersion(data.Version, wikiEntry?.MapRemoteVersions);
                    if (version == null)
                    {
                        errors.Add($"The update key '{updateKey}' matches a mod with invalid semantic version '{data.Version}'.");
                        continue;
                    }

                    if (this.IsNewer(version, main?.Version))
                        main = new ModEntryVersionModel(version, data.Url);
                }

                // handle optional version
                if (data.PreviewVersion != null)
                {
                    ISemanticVersion version = this.GetMappedVersion(data.PreviewVersion, wikiEntry?.MapRemoteVersions);
                    if (version == null)
                    {
                        errors.Add($"The update key '{updateKey}' matches a mod with invalid optional semantic version '{data.PreviewVersion}'.");
                        continue;
                    }

                    if (this.IsNewer(version, optional?.Version))
                        optional = new ModEntryVersionModel(version, data.Url);
                }
            }

            // get unofficial version
            if (wikiEntry?.Compatibility.UnofficialVersion != null && this.IsNewer(wikiEntry.Compatibility.UnofficialVersion, main?.Version) && this.IsNewer(wikiEntry.Compatibility.UnofficialVersion, optional?.Version))
                unofficial = new ModEntryVersionModel(wikiEntry.Compatibility.UnofficialVersion, $"{this.Url.PlainAction("Index", "Mods")}#{wikiEntry.Anchor}");

            // get unofficial version for beta
            if (wikiEntry?.HasBetaInfo == true)
            {
                if (wikiEntry.BetaCompatibility.Status == WikiCompatibilityStatus.Unofficial)
                {
                    if (wikiEntry.BetaCompatibility.UnofficialVersion != null)
                    {
                        unofficialForBeta = (wikiEntry.BetaCompatibility.UnofficialVersion != null && this.IsNewer(wikiEntry.BetaCompatibility.UnofficialVersion, main?.Version) && this.IsNewer(wikiEntry.BetaCompatibility.UnofficialVersion, optional?.Version))
                            ? new ModEntryVersionModel(wikiEntry.BetaCompatibility.UnofficialVersion, $"{this.Url.PlainAction("Index", "Mods")}#{wikiEntry.Anchor}")
                            : null;
                    }
                    else
                        unofficialForBeta = unofficial;
                }
            }

            // fallback to preview if latest is invalid
            if (main == null && optional != null)
            {
                main = optional;
                optional = null;
            }

            // special cases
            if (result.ID == "Pathoschild.SMAPI")
            {
                if (main != null)
                    main.Url = "https://smapi.io/";
                if (optional != null)
                    optional.Url = "https://smapi.io/";
            }

            // get recommended update (if any)
            ISemanticVersion installedVersion = this.GetMappedVersion(search.InstalledVersion?.ToString(), wikiEntry?.MapLocalVersions);
            if (apiVersion != null && installedVersion != null)
            {
                // get newer versions
                List<ModEntryVersionModel> updates = new List<ModEntryVersionModel>();
                if (this.IsRecommendedUpdate(installedVersion, main?.Version, useBetaChannel: true))
                    updates.Add(main);
                if (this.IsRecommendedUpdate(installedVersion, optional?.Version, useBetaChannel: installedVersion.IsPrerelease()))
                    updates.Add(optional);
                if (this.IsRecommendedUpdate(installedVersion, unofficial?.Version, useBetaChannel: search.IsBroken))
                    updates.Add(unofficial);
                if (this.IsRecommendedUpdate(installedVersion, unofficialForBeta?.Version, useBetaChannel: apiVersion.IsPrerelease()))
                    updates.Add(unofficialForBeta);

                // get newest version
                ModEntryVersionModel newest = null;
                foreach (ModEntryVersionModel update in updates)
                {
                    if (newest == null || update.Version.IsNewerThan(newest.Version))
                        newest = update;
                }

                // set field
                result.SuggestedUpdate = newest != null
                    ? new ModEntryVersionModel(newest.Version, newest.Url)
                    : null;
            }

            // add extended metadata
            if (includeExtendedMetadata)
                result.Metadata = new ModExtendedMetadataModel(wikiEntry, record, main: main, optional: optional, unofficial: unofficial, unofficialForBeta: unofficialForBeta);

            // add result
            result.Errors = errors.ToArray();
            return result;
        }

        /// <summary>Get whether a given version should be offered to the user as an update.</summary>
        /// <param name="currentVersion">The current semantic version.</param>
        /// <param name="newVersion">The target semantic version.</param>
        /// <param name="useBetaChannel">Whether the user enabled the beta channel and should be offered prerelease updates.</param>
        private bool IsRecommendedUpdate(ISemanticVersion currentVersion, ISemanticVersion newVersion, bool useBetaChannel)
        {
            return
                newVersion != null
                && newVersion.IsNewerThan(currentVersion)
                && (useBetaChannel || !newVersion.IsPrerelease());
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
        private IEnumerable<UpdateKey> GetUpdateKeys(string[] specifiedKeys, ModDataRecord record, WikiModEntry entry)
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
                        yield return $"{ModRepositoryKey.Nexus}:{entry.NexusID}";
                    if (entry.ModDropID.HasValue)
                        yield return $"{ModRepositoryKey.ModDrop}:{entry.ModDropID}";
                    if (entry.CurseForgeID.HasValue)
                        yield return $"{ModRepositoryKey.CurseForge}:{entry.CurseForgeID}";
                    if (entry.ChucklefishID.HasValue)
                        yield return $"{ModRepositoryKey.Chucklefish}:{entry.ChucklefishID}";
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

        /// <summary>Get a semantic local version for update checks.</summary>
        /// <param name="version">The version to parse.</param>
        /// <param name="map">A map of version replacements.</param>
        private ISemanticVersion GetMappedVersion(string version, IDictionary<string, string> map)
        {
            // try mapped version
            string rawNewVersion = this.GetRawMappedVersion(version, map);
            if (SemanticVersion.TryParse(rawNewVersion, out ISemanticVersion parsedNew))
                return parsedNew;

            // return original version
            return SemanticVersion.TryParse(version, out ISemanticVersion parsedOld)
                ? parsedOld
                : null;
        }

        /// <summary>Get a semantic local version for update checks.</summary>
        /// <param name="version">The version to map.</param>
        /// <param name="map">A map of version replacements.</param>
        private string GetRawMappedVersion(string version, IDictionary<string, string> map)
        {
            if (version == null || map == null || !map.Any())
                return version;

            // match exact raw version
            if (map.ContainsKey(version))
                return map[version];

            // match parsed version
            if (SemanticVersion.TryParse(version, out ISemanticVersion parsed))
            {
                if (map.ContainsKey(parsed.ToString()))
                    return map[parsed.ToString()];

                foreach (var pair in map)
                {
                    if (SemanticVersion.TryParse(pair.Key, out ISemanticVersion target) && parsed.Equals(target) && SemanticVersion.TryParse(pair.Value, out ISemanticVersion newVersion))
                        return newVersion.ToString();
                }
            }

            return version;
        }
    }
}
