using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Exceptions;
using StardewValley;
using xTile;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides an API for loading content assets.</summary>
    internal class ContentHelper : BaseHelper, IContentHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>SMAPI's core content logic.</summary>
        private readonly ContentCoordinator ContentCore;

        /// <summary>A content manager for this mod which manages files from the game's Content folder.</summary>
        private readonly IContentManager GameContentManager;

        /// <summary>A content manager for this mod which manages files from the mod's folder.</summary>
        private readonly ModContentManager ModContentManager;

        /// <summary>The friendly mod name for use in errors.</summary>
        private readonly string ModName;

        /// <summary>Encapsulates monitoring and logging for a given module.</summary>
        private readonly IMonitor Monitor;


        /*********
        ** Accessors
        *********/
        /// <summary>The game's current locale code (like <c>pt-BR</c>).</summary>
        public string CurrentLocale => this.GameContentManager.GetLocale();

        /// <summary>The game's current locale as an enum value.</summary>
        public LocalizedContentManager.LanguageCode CurrentLocaleConstant => this.GameContentManager.Language;

        /// <summary>The observable implementation of <see cref="AssetEditors"/>.</summary>
        internal ObservableCollection<IAssetEditor> ObservableAssetEditors { get; } = new ObservableCollection<IAssetEditor>();

        /// <summary>The observable implementation of <see cref="AssetLoaders"/>.</summary>
        internal ObservableCollection<IAssetLoader> ObservableAssetLoaders { get; } = new ObservableCollection<IAssetLoader>();

        /// <summary>Interceptors which provide the initial versions of matching content assets.</summary>
        public IList<IAssetLoader> AssetLoaders => this.ObservableAssetLoaders;

        /// <summary>Interceptors which edit matching content assets after they're loaded.</summary>
        public IList<IAssetEditor> AssetEditors => this.ObservableAssetEditors;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="contentCore">SMAPI's core content logic.</param>
        /// <param name="modFolderPath">The absolute path to the mod folder.</param>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="modName">The friendly mod name for use in errors.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public ContentHelper(ContentCoordinator contentCore, string modFolderPath, string modID, string modName, IMonitor monitor)
            : base(modID)
        {
            this.ContentCore = contentCore;
            this.GameContentManager = contentCore.CreateGameContentManager(this.ContentCore.GetManagedAssetPrefix(modID) + ".content");
            this.ModContentManager = contentCore.CreateModContentManager(this.ContentCore.GetManagedAssetPrefix(modID), modFolderPath, this.GameContentManager);
            this.ModName = modName;
            this.Monitor = monitor;
        }

        /// <summary>Load content from the game folder or mod folder (if not already cached), and return it. When loading a <c>.png</c> file, this must be called outside the game's draw loop.</summary>
        /// <typeparam name="T">The expected data type. The main supported types are <see cref="Map"/>, <see cref="Texture2D"/>, and dictionaries; other types may be supported by the game's content pipeline.</typeparam>
        /// <param name="key">The asset key to fetch (if the <paramref name="source"/> is <see cref="ContentSource.GameContent"/>), or the local path to a content file relative to the mod folder.</param>
        /// <param name="source">Where to search for a matching content asset.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        /// <exception cref="ContentLoadException">The content asset couldn't be loaded (e.g. because it doesn't exist).</exception>
        public T Load<T>(string key, ContentSource source = ContentSource.ModFolder)
        {
            try
            {
                this.AssertAndNormalizeAssetName(key);
                switch (source)
                {
                    case ContentSource.GameContent:
                        return this.GameContentManager.Load<T>(key, this.CurrentLocaleConstant, useCache: false);

                    case ContentSource.ModFolder:
                        return this.ModContentManager.Load<T>(key, Constants.DefaultLanguage, useCache: false);

                    default:
                        throw new SContentLoadException($"{this.ModName} failed loading content asset '{key}' from {source}: unknown content source '{source}'.");
                }
            }
            catch (Exception ex) when (!(ex is SContentLoadException))
            {
                throw new SContentLoadException($"{this.ModName} failed loading content asset '{key}' from {source}.", ex);
            }
        }

        /// <summary>Normalize an asset name so it's consistent with those generated by the game. This is mainly useful for string comparisons like <see cref="string.StartsWith(string)"/> on generated asset names, and isn't necessary when passing asset names into other content helper methods.</summary>
        /// <param name="assetName">The asset key.</param>
        [Pure]
        public string NormalizeAssetName(string assetName)
        {
            return this.ModContentManager.AssertAndNormalizeAssetName(assetName);
        }

        /// <summary>Get the underlying key in the game's content cache for an asset. This can be used to load custom map tilesheets, but should be avoided when you can use the content API instead. This does not validate whether the asset exists.</summary>
        /// <param name="key">The asset key to fetch (if the <paramref name="source"/> is <see cref="ContentSource.GameContent"/>), or the local path to a content file relative to the mod folder.</param>
        /// <param name="source">Where to search for a matching content asset.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        public string GetActualAssetKey(string key, ContentSource source = ContentSource.ModFolder)
        {
            switch (source)
            {
                case ContentSource.GameContent:
                    return this.GameContentManager.AssertAndNormalizeAssetName(key);

                case ContentSource.ModFolder:
                    return this.ModContentManager.GetInternalAssetKey(key);

                default:
                    throw new NotSupportedException($"Unknown content source '{source}'.");
            }
        }

        /// <summary>Remove an asset from the content cache so it's reloaded on the next request. This will reload core game assets if needed, but references to the former asset will still show the previous content.</summary>
        /// <param name="key">The asset key to invalidate in the content folder.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        /// <returns>Returns whether the given asset key was cached.</returns>
        public bool InvalidateCache(string key)
        {
            string actualKey = this.GetActualAssetKey(key, ContentSource.GameContent);
            this.Monitor.Log($"Requested cache invalidation for '{actualKey}'.", LogLevel.Trace);
            return this.ContentCore.InvalidateCache(asset => asset.AssetNameEquals(actualKey)).Any();
        }

        /// <summary>Remove all assets of the given type from the cache so they're reloaded on the next request. <b>This can be a very expensive operation and should only be used in very specific cases.</b> This will reload core game assets if needed, but references to the former assets will still show the previous content.</summary>
        /// <typeparam name="T">The asset type to remove from the cache.</typeparam>
        /// <returns>Returns whether any assets were invalidated.</returns>
        public bool InvalidateCache<T>()
        {
            this.Monitor.Log($"Requested cache invalidation for all assets of type {typeof(T)}. This is an expensive operation and should be avoided if possible.", LogLevel.Trace);
            return this.ContentCore.InvalidateCache((key, type) => typeof(T).IsAssignableFrom(type)).Any();
        }

        /// <summary>Remove matching assets from the content cache so they're reloaded on the next request. This will reload core game assets if needed, but references to the former asset will still show the previous content.</summary>
        /// <param name="predicate">A predicate matching the assets to invalidate.</param>
        /// <returns>Returns whether any cache entries were invalidated.</returns>
        public bool InvalidateCache(Func<IAssetInfo, bool> predicate)
        {
            this.Monitor.Log("Requested cache invalidation for all assets matching a predicate.", LogLevel.Trace);
            return this.ContentCore.InvalidateCache(predicate).Any();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Assert that the given key has a valid format.</summary>
        /// <param name="key">The asset key to check.</param>
        /// <exception cref="ArgumentException">The asset key is empty or contains invalid characters.</exception>
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Parameter is only used for assertion checks by design.")]
        private void AssertAndNormalizeAssetName(string key)
        {
            this.ModContentManager.AssertAndNormalizeAssetName(key);
            if (Path.IsPathRooted(key))
                throw new ArgumentException("The asset key must not be an absolute path.");
        }
    }
}
