using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Utilities;
using StardewValley;

namespace StardewModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files from the game content folder with support for interception.</summary>
    internal class GameContentManager : BaseContentManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assets currently being intercepted by <see cref="IAssetLoader"/> instances. This is used to prevent infinite loops when a loader loads a new asset.</summary>
        private readonly ContextHash<string> AssetsBeingLoaded = new ContextHash<string>();

        /// <summary>Interceptors which provide the initial versions of matching assets.</summary>
        private IDictionary<IModMetadata, IList<IAssetLoader>> Loaders => this.Coordinator.Loaders;

        /// <summary>Interceptors which edit matching assets after they're loaded.</summary>
        private IDictionary<IModMetadata, IList<IAssetEditor>> Editors => this.Coordinator.Editors;

        /// <summary>A lookup which indicates whether the asset is localizable (i.e. the filename contains the locale), if previously loaded.</summary>
        private readonly IDictionary<string, bool> IsLocalizableLookup;

        /// <summary>Whether the next load is the first for any game content manager.</summary>
        private static bool IsFirstLoad = true;

        /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
        private readonly Action OnLoadingFirstAsset;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localize content.</param>
        /// <param name="coordinator">The central coordinator which manages content managers.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="onDisposing">A callback to invoke when the content manager is being disposed.</param>
        /// <param name="onLoadingFirstAsset">A callback to invoke the first time *any* game content manager loads an asset.</param>
        public GameContentManager(string name, IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, Action<BaseContentManager> onDisposing, Action onLoadingFirstAsset)
            : base(name, serviceProvider, rootDirectory, currentCulture, coordinator, monitor, reflection, onDisposing, isNamespaced: false)
        {
            this.IsLocalizableLookup = reflection.GetField<IDictionary<string, bool>>(this, "_localizedAsset").GetValue();
            this.OnLoadingFirstAsset = onLoadingFirstAsset;
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="language">The language code for which to load content.</param>
        /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
        public override T Load<T>(string assetName, LocalizedContentManager.LanguageCode language, bool useCache)
        {
            // raise first-load callback
            if (GameContentManager.IsFirstLoad)
            {
                GameContentManager.IsFirstLoad = false;
                this.OnLoadingFirstAsset();
            }

            // normalize asset name
            assetName = this.AssertAndNormalizeAssetName(assetName);
            if (this.TryParseExplicitLanguageAssetKey(assetName, out string newAssetName, out LanguageCode newLanguage))
                return this.Load<T>(newAssetName, newLanguage, useCache);

            // get from cache
            if (useCache && this.IsLoaded(assetName))
                return this.RawLoad<T>(assetName, language, useCache: true);

            // get managed asset
            if (this.Coordinator.TryParseManagedAssetKey(assetName, out string contentManagerID, out string relativePath))
            {
                T managedAsset = this.Coordinator.LoadManagedAsset<T>(contentManagerID, relativePath);
                if (useCache)
                    this.Inject(assetName, managedAsset, language);
                return managedAsset;
            }

            // load asset
            T data;
            if (this.AssetsBeingLoaded.Contains(assetName))
            {
                this.Monitor.Log($"Broke loop while loading asset '{assetName}'.", LogLevel.Warn);
                this.Monitor.Log($"Bypassing mod loaders for this asset. Stack trace:\n{Environment.StackTrace}", LogLevel.Trace);
                data = this.RawLoad<T>(assetName, language, useCache);
            }
            else
            {
                data = this.AssetsBeingLoaded.Track(assetName, () =>
                {
                    string locale = this.GetLocale(language);
                    IAssetInfo info = new AssetInfo(locale, assetName, typeof(T), this.AssertAndNormalizeAssetName);
                    IAssetData asset =
                        this.ApplyLoader<T>(info)
                        ?? new AssetDataForObject(info, this.RawLoad<T>(assetName, language, useCache), this.AssertAndNormalizeAssetName);
                    asset = this.ApplyEditors<T>(info, asset);
                    return (T)asset.Data;
                });
            }

            // update cache & return data
            this.Inject(assetName, data, language);
            return data;
        }

        /// <summary>Perform any cleanup needed when the locale changes.</summary>
        public override void OnLocaleChanged()
        {
            base.OnLocaleChanged();

            // find assets for which a translatable version was loaded
            HashSet<string> removeAssetNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (string key in this.IsLocalizableLookup.Where(p => p.Value).Select(p => p.Key))
                removeAssetNames.Add(this.TryParseExplicitLanguageAssetKey(key, out string assetName, out _) ? assetName : key);

            // invalidate translatable assets
            string[] invalidated = this
                .InvalidateCache((key, type) =>
                    removeAssetNames.Contains(key)
                    || (this.TryParseExplicitLanguageAssetKey(key, out string assetName, out _) && removeAssetNames.Contains(assetName))
                )
                .Select(p => p.Item1)
                .OrderBy(p => p, StringComparer.InvariantCultureIgnoreCase)
                .ToArray();
            if (invalidated.Any())
                this.Monitor.Log($"Invalidated {invalidated.Length} asset names: {string.Join(", ", invalidated)} for locale change.", LogLevel.Trace);
        }

        /// <summary>Create a new content manager for temporary use.</summary>
        public override LocalizedContentManager CreateTemporary()
        {
            return this.Coordinator.CreateGameContentManager("(temporary)");
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether an asset has already been loaded.</summary>
        /// <param name="normalizedAssetName">The normalized asset name.</param>
        protected override bool IsNormalizedKeyLoaded(string normalizedAssetName)
        {
            // default English
            if (this.Language == LocalizedContentManager.LanguageCode.en || this.Coordinator.IsManagedAssetKey(normalizedAssetName))
                return this.Cache.ContainsKey(normalizedAssetName);

            // translated
            string keyWithLocale = $"{normalizedAssetName}.{this.GetLocale(this.GetCurrentLanguage())}";
            if (this.IsLocalizableLookup.TryGetValue(keyWithLocale, out bool localizable))
            {
                return localizable
                    ? this.Cache.ContainsKey(keyWithLocale)
                    : this.Cache.ContainsKey(normalizedAssetName);
            }

            // not loaded yet
            return false;
        }

        /// <summary>Inject an asset into the cache.</summary>
        /// <typeparam name="T">The type of asset to inject.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="value">The asset value.</param>
        /// <param name="language">The language code for which to inject the asset.</param>
        protected override void Inject<T>(string assetName, T value, LanguageCode language)
        {
            // handle explicit language in asset name
            {
                if (this.TryParseExplicitLanguageAssetKey(assetName, out string newAssetName, out LanguageCode newLanguage))
                {
                    this.Inject(newAssetName, value, newLanguage);
                    return;
                }
            }

            // save to cache
            // Note: even if the asset was loaded and cached right before this method was called,
            // we need to fully re-inject it here for two reasons:
            //   1. So we can look up an asset by its base or localized key (the game/XNA logic
            //      only caches by the most specific key).
            //   2. Because a mod asset loader/editor may have changed the asset in a way that
            //      doesn't change the instance stored in the cache, e.g. using `asset.ReplaceWith`.
            string keyWithLocale = $"{assetName}.{this.GetLocale(language)}";
            base.Inject(assetName, value, language);
            if (this.Cache.ContainsKey(keyWithLocale))
                base.Inject(keyWithLocale, value, language);

            // track whether the injected asset is translatable for is-loaded lookups
            if (this.Cache.ContainsKey(keyWithLocale))
            {
                this.IsLocalizableLookup[assetName] = true;
                this.IsLocalizableLookup[keyWithLocale] = true;
            }
            else if (this.Cache.ContainsKey(assetName))
            {
                this.IsLocalizableLookup[assetName] = false;
                this.IsLocalizableLookup[keyWithLocale] = false;
            }
            else
                this.Monitor.Log($"Asset '{assetName}' could not be found in the cache immediately after injection.", LogLevel.Error);
        }

        /// <summary>Load an asset file directly from the underlying content manager.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The normalized asset key.</param>
        /// <param name="language">The language code for which to load content.</param>
        /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
        /// <remarks>Derived from <see cref="LocalizedContentManager.Load{T}(string, LocalizedContentManager.LanguageCode)"/>.</remarks>
        private T RawLoad<T>(string assetName, LanguageCode language, bool useCache)
        {
            // try translated asset
            if (language != LocalizedContentManager.LanguageCode.en)
            {
                string translatedKey = $"{assetName}.{this.GetLocale(language)}";
                if (!this.IsLocalizableLookup.TryGetValue(translatedKey, out bool isTranslatable) || isTranslatable)
                {
                    try
                    {
                        T obj = base.RawLoad<T>(translatedKey, useCache);
                        this.IsLocalizableLookup[assetName] = true;
                        this.IsLocalizableLookup[translatedKey] = true;
                        return obj;
                    }
                    catch (ContentLoadException)
                    {
                        this.IsLocalizableLookup[assetName] = false;
                        this.IsLocalizableLookup[translatedKey] = false;
                    }
                }
            }

            // try base asset
            return base.RawLoad<T>(assetName, useCache);
        }

        /// <summary>Parse an asset key that contains an explicit language into its asset name and language, if applicable.</summary>
        /// <param name="rawAsset">The asset key to parse.</param>
        /// <param name="assetName">The asset name without the language code.</param>
        /// <param name="language">The language code removed from the asset name.</param>
        /// <returns>Returns whether the asset key contains an explicit language and was successfully parsed.</returns>
        private bool TryParseExplicitLanguageAssetKey(string rawAsset, out string assetName, out LanguageCode language)
        {
            if (string.IsNullOrWhiteSpace(rawAsset))
                throw new SContentLoadException("The asset key is empty.");

            // extract language code
            int splitIndex = rawAsset.LastIndexOf('.');
            if (splitIndex != -1 && this.LanguageCodes.TryGetValue(rawAsset.Substring(splitIndex + 1), out language))
            {
                assetName = rawAsset.Substring(0, splitIndex);
                return true;
            }

            // no explicit language code found
            assetName = rawAsset;
            language = this.Language;
            return false;
        }

        /// <summary>Load the initial asset from the registered <see cref="Loaders"/>.</summary>
        /// <param name="info">The basic asset metadata.</param>
        /// <returns>Returns the loaded asset metadata, or <c>null</c> if no loader matched.</returns>
        private IAssetData ApplyLoader<T>(IAssetInfo info)
        {
            // find matching loaders
            var loaders = this.GetInterceptors(this.Loaders)
                .Where(entry =>
                {
                    try
                    {
                        return entry.Value.CanLoad<T>(info);
                    }
                    catch (Exception ex)
                    {
                        entry.Key.LogAsMod($"Mod failed when checking whether it could load asset '{info.AssetName}', and will be ignored. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                        return false;
                    }
                })
                .ToArray();

            // validate loaders
            if (!loaders.Any())
                return null;
            if (loaders.Length > 1)
            {
                string[] loaderNames = loaders.Select(p => p.Key.DisplayName).ToArray();
                this.Monitor.Log($"Multiple mods want to provide the '{info.AssetName}' asset ({string.Join(", ", loaderNames)}), but an asset can't be loaded multiple times. SMAPI will use the default asset instead; uninstall one of the mods to fix this. (Message for modders: you should usually use {typeof(IAssetEditor)} instead to avoid conflicts.)", LogLevel.Warn);
                return null;
            }

            // fetch asset from loader
            IModMetadata mod = loaders[0].Key;
            IAssetLoader loader = loaders[0].Value;
            T data;
            try
            {
                data = loader.Load<T>(info);
                this.Monitor.Log($"{mod.DisplayName} loaded asset '{info.AssetName}'.", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                mod.LogAsMod($"Mod crashed when loading asset '{info.AssetName}'. SMAPI will use the default asset instead. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                return null;
            }

            // validate asset
            if (data == null)
            {
                mod.LogAsMod($"Mod incorrectly set asset '{info.AssetName}' to a null value; ignoring override.", LogLevel.Error);
                return null;
            }

            // return matched asset
            return new AssetDataForObject(info, data, this.AssertAndNormalizeAssetName);
        }

        /// <summary>Apply any <see cref="Editors"/> to a loaded asset.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="info">The basic asset metadata.</param>
        /// <param name="asset">The loaded asset.</param>
        private IAssetData ApplyEditors<T>(IAssetInfo info, IAssetData asset)
        {
            IAssetData GetNewData(object data) => new AssetDataForObject(info, data, this.AssertAndNormalizeAssetName);

            // edit asset
            foreach (var entry in this.GetInterceptors(this.Editors))
            {
                // check for match
                IModMetadata mod = entry.Key;
                IAssetEditor editor = entry.Value;
                try
                {
                    if (!editor.CanEdit<T>(info))
                        continue;
                }
                catch (Exception ex)
                {
                    mod.LogAsMod($"Mod crashed when checking whether it could edit asset '{info.AssetName}', and will be ignored. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                    continue;
                }

                // try edit
                object prevAsset = asset.Data;
                try
                {
                    editor.Edit<T>(asset);
                    this.Monitor.Log($"{mod.DisplayName} edited {info.AssetName}.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    mod.LogAsMod($"Mod crashed when editing asset '{info.AssetName}', which may cause errors in-game. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }

                // validate edit
                if (asset.Data == null)
                {
                    mod.LogAsMod($"Mod incorrectly set asset '{info.AssetName}' to a null value; ignoring override.", LogLevel.Warn);
                    asset = GetNewData(prevAsset);
                }
                else if (!(asset.Data is T))
                {
                    mod.LogAsMod($"Mod incorrectly set asset '{asset.AssetName}' to incompatible type '{asset.Data.GetType()}', expected '{typeof(T)}'; ignoring override.", LogLevel.Warn);
                    asset = GetNewData(prevAsset);
                }
            }

            // return result
            return asset;
        }

        /// <summary>Get all registered interceptors from a list.</summary>
        private IEnumerable<KeyValuePair<IModMetadata, T>> GetInterceptors<T>(IDictionary<IModMetadata, IList<T>> entries)
        {
            foreach (var entry in entries)
            {
                IModMetadata mod = entry.Key;
                IList<T> interceptors = entry.Value;

                // registered editors
                foreach (T interceptor in interceptors)
                    yield return new KeyValuePair<IModMetadata, T>(mod, interceptor);
            }
        }
    }
}
