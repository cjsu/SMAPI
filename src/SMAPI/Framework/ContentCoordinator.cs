using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Metadata;
using StardewModdingAPI.Toolkit.Serialisation;
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

        /// <summary>Whether the content coordinator has been disposed.</summary>
        private bool IsDisposed;


        /*********
        ** Accessors
        *********/
        /// <summary>The primary content manager used for most assets.</summary>
        public GameContentManager MainContentManager { get; private set; }

        /// <summary>The current language as a constant.</summary>
        public LocalizedContentManager.LanguageCode Language => this.MainContentManager.Language;

        /// <summary>Interceptors which provide the initial versions of matching assets.</summary>
        public IDictionary<IModMetadata, IList<IAssetLoader>> Loaders { get; } = new Dictionary<IModMetadata, IList<IAssetLoader>>();

        /// <summary>Interceptors which edit matching assets after they're loaded.</summary>
        public IDictionary<IModMetadata, IList<IAssetEditor>> Editors { get; } = new Dictionary<IModMetadata, IList<IAssetEditor>>();

        /// <summary>The absolute path to the <see cref="ContentManager.RootDirectory"/>.</summary>
        public string FullRootDirectory { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localise content.</param>
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
            this.CoreAssets = new CoreAssetPropagator(this.MainContentManager.AssertAndNormaliseAssetName, reflection, monitor);
        }

        /// <summary>Get a new content manager which handles reading files from the game content folder with support for interception.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        public GameContentManager CreateGameContentManager(string name)
        {
            GameContentManager manager = new GameContentManager(name, this.MainContentManager.ServiceProvider, this.MainContentManager.RootDirectory, this.MainContentManager.CurrentCulture, this, this.Monitor, this.Reflection, this.OnDisposing, this.OnLoadingFirstAsset);
            this.ContentManagers.Add(manager);
            return manager;
        }

        /// <summary>Get a new content manager which handles reading files from a SMAPI mod folder with support for unpacked files.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        /// <param name="rootDirectory">The root directory to search for content (or <c>null</c> for the default).</param>
        public ModContentManager CreateModContentManager(string name, string rootDirectory)
        {
            ModContentManager manager = new ModContentManager(name, this.MainContentManager.ServiceProvider, rootDirectory, this.MainContentManager.CurrentCulture, this, this.Monitor, this.Reflection, this.JsonHelper, this.OnDisposing);
            this.ContentManagers.Add(manager);
            return manager;
        }

        /// <summary>Get the current content locale.</summary>
        public string GetLocale()
        {
            return this.MainContentManager.GetLocale(LocalizedContentManager.CurrentLanguageCode);
        }

        /// <summary>Perform any cleanup needed when the locale changes.</summary>
        public void OnLocaleChanged()
        {
            foreach (IContentManager contentManager in this.ContentManagers)
                contentManager.OnLocaleChanged();
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
        /// <param name="internalKey">The internal asset key.</param>
        /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
        /// <param name="relativePath">The internal SMAPI asset key.</param>
        /// <param name="language">The language code for which to load content.</param>
        public T LoadAndCloneManagedAsset<T>(string internalKey, string contentManagerID, string relativePath, LocalizedContentManager.LanguageCode language)
        {
            // get content manager
            IContentManager contentManager = this.ContentManagers.FirstOrDefault(p => p.Name == contentManagerID);
            if (contentManager == null)
                throw new InvalidOperationException($"The '{contentManagerID}' prefix isn't handled by any mod.");

            // get cloned asset
            T data = contentManager.Load<T>(internalKey, language);
            return contentManager.CloneIfPossible(data);
        }

        /// <summary>Purge assets from the cache that match one of the interceptors.</summary>
        /// <param name="editors">The asset editors for which to purge matching assets.</param>
        /// <param name="loaders">The asset loaders for which to purge matching assets.</param>
        /// <returns>Returns the invalidated asset names.</returns>
        public IEnumerable<string> InvalidateCacheFor(IAssetEditor[] editors, IAssetLoader[] loaders)
        {
            if (!editors.Any() && !loaders.Any())
                return new string[0];

            // get CanEdit/Load methods
            MethodInfo canEdit = typeof(IAssetEditor).GetMethod(nameof(IAssetEditor.CanEdit));
            MethodInfo canLoad = typeof(IAssetLoader).GetMethod(nameof(IAssetLoader.CanLoad));
            if (canEdit == null || canLoad == null)
                throw new InvalidOperationException("SMAPI could not access the interceptor methods."); // should never happen

            // invalidate matching keys
            return this.InvalidateCache(asset =>
            {
                // check loaders
                MethodInfo canLoadGeneric = canLoad.MakeGenericMethod(asset.DataType);
                foreach (IAssetLoader loader in loaders)
                {
                    try
                    {
                        if ((bool)canLoadGeneric.Invoke(loader, new object[] { asset }))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        this.GetModFor(loader).LogAsMod($"Mod failed when checking whether it could load asset '{asset.AssetName}'. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                    }
                }

                // check editors
                MethodInfo canEditGeneric = canEdit.MakeGenericMethod(asset.DataType);
                foreach (IAssetEditor editor in editors)
                {
                    try
                    {
                        if ((bool)canEditGeneric.Invoke(editor, new object[] { asset }))
                            return true;
                    }
                    catch (Exception ex)
                    {
                        this.GetModFor(editor).LogAsMod($"Mod failed when checking whether it could edit asset '{asset.AssetName}'. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                    }
                }

                // asset not affected by a loader or editor
                return false;
            });
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
                IAssetInfo info = new AssetInfo(locale, assetName, type, this.MainContentManager.AssertAndNormaliseAssetName);
                return predicate(info);
            }, dispose);
        }

        /// <summary>Purge matched assets from the cache.</summary>
        /// <param name="predicate">Matches the asset keys to invalidate.</param>
        /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
        /// <returns>Returns the invalidated asset names.</returns>
        public IEnumerable<string> InvalidateCache(Func<string, Type, bool> predicate, bool dispose = false)
        {
            // invalidate cache
            IDictionary<string, Type> removedAssetNames = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
            foreach (IContentManager contentManager in this.ContentManagers)
            {
                foreach (Tuple<string, Type> asset in contentManager.InvalidateCache(predicate, dispose))
                    removedAssetNames[asset.Item1] = asset.Item2;
            }

            // reload core game assets
            int reloaded = 0;
            foreach (var pair in removedAssetNames)
            {
                string key = pair.Key;
                Type type = pair.Value;
                if (this.CoreAssets.Propagate(this.MainContentManager, key, type)) // use an intercepted content manager
                    reloaded++;
            }

            // report result
            if (removedAssetNames.Any())
                this.Monitor.Log($"Invalidated {removedAssetNames.Count} asset names: {string.Join(", ", removedAssetNames.Keys.OrderBy(p => p, StringComparer.InvariantCultureIgnoreCase))}. Reloaded {reloaded} core assets.", LogLevel.Trace);
            else
                this.Monitor.Log("Invalidated 0 cache entries.", LogLevel.Trace);

            return removedAssetNames.Keys;
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

            this.ContentManagers.Remove(contentManager);
        }

        /// <summary>Get the mod which registered an asset loader.</summary>
        /// <param name="loader">The asset loader.</param>
        /// <exception cref="KeyNotFoundException">The given loader couldn't be matched to a mod.</exception>
        private IModMetadata GetModFor(IAssetLoader loader)
        {
            foreach (var pair in this.Loaders)
            {
                if (pair.Value.Contains(loader))
                    return pair.Key;
            }

            throw new KeyNotFoundException("This loader isn't associated with a known mod.");
        }

        /// <summary>Get the mod which registered an asset editor.</summary>
        /// <param name="editor">The asset editor.</param>
        /// <exception cref="KeyNotFoundException">The given editor couldn't be matched to a mod.</exception>
        private IModMetadata GetModFor(IAssetEditor editor)
        {
            foreach (var pair in this.Editors)
            {
                if (pair.Value.Contains(editor))
                    return pair.Key;
            }

            throw new KeyNotFoundException("This editor isn't associated with a known mod.");
        }
    }
}
