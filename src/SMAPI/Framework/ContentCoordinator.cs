using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewModdingAPI.Metadata;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>The central logic for creating content managers, invalidating caches, and propagating asset changes.</summary>
    internal class ContentCoordinator : IDisposable
    {
        /*********
        ** Fields
        *********/
        /// <summary>An asset key prefix for assets from SMAPI mod folders.</summary>
        private readonly string ManagedPrefix = "SMAPI";

        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>Provides metadata for core game assets.</summary>
        private readonly CoreAssetPropagator CoreAssets;

        /// <summary>Simplifies access to private code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;

        /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
        private readonly Action OnLoadingFirstAsset;

        /// <summary>The loaded content managers (including the <see cref="MainContentManager"/>).</summary>
        private readonly IList<IContentManager> ContentManagers = new List<IContentManager>();

        /// <summary>The language code for language-agnostic mod assets.</summary>
        private readonly LocalizedContentManager.LanguageCode DefaultLanguage = Constants.DefaultLanguage;

        /// <summary>Whether the content coordinator has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>A lock used to prevent asynchronous changes to the content manager list.</summary>
        /// <remarks>The game may adds content managers in asynchronous threads (e.g. when populating the load screen).</remarks>
        private readonly ReaderWriterLockSlim ContentManagerLock = new ReaderWriterLockSlim();


        /*********
        ** Accessors
        *********/
        /// <summary>The primary content manager used for most assets.</summary>
        public GameContentManager MainContentManager { get; private set; }

        /// <summary>The current language as a constant.</summary>
        public LocalizedContentManager.LanguageCode Language => this.MainContentManager.Language;

        /// <summary>Interceptors which provide the initial versions of matching assets.</summary>
        public IList<ModLinked<IAssetLoader>> Loaders { get; } = new List<ModLinked<IAssetLoader>>();

        /// <summary>Interceptors which edit matching assets after they're loaded.</summary>
        public IList<ModLinked<IAssetEditor>> Editors { get; } = new List<ModLinked<IAssetEditor>>();

        /// <summary>The absolute path to the <see cref="ContentManager.RootDirectory"/>.</summary>
        public string FullRootDirectory { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localize content.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        /// <param name="onLoadingFirstAsset">A callback to invoke the first time *any* game content manager loads an asset.</param>
        public ContentCoordinator(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, IMonitor monitor, Reflector reflection, JsonHelper jsonHelper, Action onLoadingFirstAsset)
        {
            this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            this.Reflection = reflection;
            this.JsonHelper = jsonHelper;
            this.OnLoadingFirstAsset = onLoadingFirstAsset;
            this.FullRootDirectory = Path.Combine(Constants.ExecutionPath, rootDirectory);
            this.ContentManagers.Add(
                this.MainContentManager = new GameContentManager("Game1.content", serviceProvider, rootDirectory, currentCulture, this, monitor, reflection, this.OnDisposing, onLoadingFirstAsset)
            );
            this.CoreAssets = new CoreAssetPropagator(this.MainContentManager.AssertAndNormalizeAssetName, reflection, monitor);
        }

        /// <summary>Get a new content manager which handles reading files from the game content folder with support for interception.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        public GameContentManager CreateGameContentManager(string name)
        {
            return this.ContentManagerLock.InWriteLock(() =>
            {
                GameContentManager manager = new GameContentManager(name, this.MainContentManager.ServiceProvider, this.MainContentManager.RootDirectory, this.MainContentManager.CurrentCulture, this, this.Monitor, this.Reflection, this.OnDisposing, this.OnLoadingFirstAsset);
                this.ContentManagers.Add(manager);
                return manager;
            });
        }

        /// <summary>Get a new content manager which handles reading files from a SMAPI mod folder with support for unpacked files.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        /// <param name="modName">The mod display name to show in errors.</param>
        /// <param name="rootDirectory">The root directory to search for content (or <c>null</c> for the default).</param>
        /// <param name="gameContentManager">The game content manager used for map tilesheets not provided by the mod.</param>
        public ModContentManager CreateModContentManager(string name, string modName, string rootDirectory, IContentManager gameContentManager)
        {
            return this.ContentManagerLock.InWriteLock(() =>
            {
                ModContentManager manager = new ModContentManager(
                    name: name,
                    gameContentManager: gameContentManager,
                    serviceProvider: this.MainContentManager.ServiceProvider,
                    rootDirectory: rootDirectory,
                    modName: modName,
                    currentCulture: this.MainContentManager.CurrentCulture,
                    coordinator: this,
                    monitor: this.Monitor,
                    reflection: this.Reflection,
                    jsonHelper: this.JsonHelper,
                    onDisposing: this.OnDisposing
                );
                this.ContentManagers.Add(manager);
                return manager;
            });
        }

        /// <summary>Get the current content locale.</summary>
        public string GetLocale()
        {
            return this.MainContentManager.GetLocale(LocalizedContentManager.CurrentLanguageCode);
        }

        /// <summary>Perform any cleanup needed when the locale changes.</summary>
        public void OnLocaleChanged()
        {
            this.ContentManagerLock.InReadLock(() =>
            {
                foreach (IContentManager contentManager in this.ContentManagers)
                    contentManager.OnLocaleChanged();
            });
        }

        /// <summary>Get whether this asset is mapped to a mod folder.</summary>
        /// <param name="key">The asset key.</param>
        public bool IsManagedAssetKey(string key)
        {
            return key.StartsWith(this.ManagedPrefix);
        }

        /// <summary>Parse a managed SMAPI asset key which maps to a mod folder.</summary>
        /// <param name="key">The asset key.</param>
        /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
        /// <param name="relativePath">The relative path within the mod folder.</param>
        /// <returns>Returns whether the asset was parsed successfully.</returns>
        public bool TryParseManagedAssetKey(string key, out string contentManagerID, out string relativePath)
        {
            contentManagerID = null;
            relativePath = null;

            // not a managed asset
            if (!key.StartsWith(this.ManagedPrefix))
                return false;

            // parse
            string[] parts = PathUtilities.GetSegments(key, 3);
            if (parts.Length != 3) // managed key prefix, mod id, relative path
                return false;
            contentManagerID = Path.Combine(parts[0], parts[1]);
            relativePath = parts[2];
            return true;
        }

        /// <summary>Get the managed asset key prefix for a mod.</summary>
        /// <param name="modID">The mod's unique ID.</param>
        public string GetManagedAssetPrefix(string modID)
        {
            return Path.Combine(this.ManagedPrefix, modID.ToLower());
        }

        /// <summary>Get a copy of an asset from a mod folder.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
        /// <param name="relativePath">The internal SMAPI asset key.</param>
        public T LoadManagedAsset<T>(string contentManagerID, string relativePath)
        {
            // get content manager
            IContentManager contentManager = this.ContentManagerLock.InReadLock(() =>
                this.ContentManagers.FirstOrDefault(p => p.IsNamespaced && p.Name == contentManagerID)
            );
            if (contentManager == null)
                throw new InvalidOperationException($"The '{contentManagerID}' prefix isn't handled by any mod.");

            // get fresh asset
            return contentManager.Load<T>(relativePath, this.DefaultLanguage, useCache: false);
        }

        /// <summary>Purge matched assets from the cache.</summary>
        /// <param name="predicate">Matches the asset keys to invalidate.</param>
        /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
        /// <returns>Returns the invalidated asset keys.</returns>
        public IEnumerable<string> InvalidateCache(Func<IAssetInfo, bool> predicate, bool dispose = false)
        {
            string locale = this.GetLocale();
            return this.InvalidateCache((assetName, type) =>
            {
                IAssetInfo info = new AssetInfo(locale, assetName, type, this.MainContentManager.AssertAndNormalizeAssetName);
                return predicate(info);
            }, dispose);
        }

        /// <summary>Purge matched assets from the cache.</summary>
        /// <param name="predicate">Matches the asset keys to invalidate.</param>
        /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
        /// <returns>Returns the invalidated asset names.</returns>
        public IEnumerable<string> InvalidateCache(Func<string, Type, bool> predicate, bool dispose = false)
        {
            // invalidate cache & track removed assets
            IDictionary<string, ISet<object>> removedAssets = new Dictionary<string, ISet<object>>(StringComparer.InvariantCultureIgnoreCase);
            this.ContentManagerLock.InReadLock(() =>
            {
                foreach (IContentManager contentManager in this.ContentManagers)
                {
                    foreach (var entry in contentManager.InvalidateCache(predicate, dispose))
                    {
                        if (!removedAssets.TryGetValue(entry.Key, out ISet<object> assets))
                            removedAssets[entry.Key] = assets = new HashSet<object>(new ObjectReferenceComparer<object>());
                        assets.Add(entry.Value);
                    }
                }
            });

            // reload core game assets
            if (removedAssets.Any())
            {
                IDictionary<string, bool> propagated = this.CoreAssets.Propagate(this.MainContentManager, removedAssets.ToDictionary(p => p.Key, p => p.Value.First().GetType())); // use an intercepted content manager
                this.Monitor.Log($"Invalidated {removedAssets.Count} asset names ({string.Join(", ", removedAssets.Keys.OrderBy(p => p, StringComparer.InvariantCultureIgnoreCase))}); propagated {propagated.Count(p => p.Value)} core assets.", LogLevel.Trace);
            }
            else
                this.Monitor.Log("Invalidated 0 cache entries.", LogLevel.Trace);

            return removedAssets.Keys;
        }

        /// <summary>Get all loaded instances of an asset name.</summary>
        /// <param name="assetName">The asset name.</param>
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "This method is provided for Content Patcher.")]
        public IEnumerable<object> GetLoadedValues(string assetName)
        {
            return this.ContentManagerLock.InReadLock(() =>
            {
                List<object> values = new List<object>();
                foreach (IContentManager content in this.ContentManagers.Where(p => !p.IsNamespaced && p.IsLoaded(assetName)))
                {
                    object value = content.Load<object>(assetName, this.Language, useCache: true);
                    values.Add(value);
                }
                return values;
            });
        }

        /// <summary>Dispose held resources.</summary>
        public void Dispose()
        {
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;

            this.Monitor.Log("Disposing the content coordinator. Content managers will no longer be usable after this point.", LogLevel.Trace);
            foreach (IContentManager contentManager in this.ContentManagers)
                contentManager.Dispose();
            this.ContentManagers.Clear();
            this.MainContentManager = null;

            this.ContentManagerLock.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>A callback invoked when a content manager is disposed.</summary>
        /// <param name="contentManager">The content manager being disposed.</param>
        private void OnDisposing(IContentManager contentManager)
        {
            if (this.IsDisposed)
                return;

            this.ContentManagerLock.InWriteLock(() =>
                this.ContentManagers.Remove(contentManager)
            );
        }
    }
}
