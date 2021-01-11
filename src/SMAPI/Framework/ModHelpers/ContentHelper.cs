using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.ContentManagers;
using StardewModdingAPI.Framework.Exceptions;
using StardewValley;

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

        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string CurrentLocale => this.GameContentManager.GetLocale();

        /// <inheritdoc />
        public LocalizedContentManager.LanguageCode CurrentLocaleConstant => this.GameContentManager.Language;

        /// <summary>The observable implementation of <see cref="AssetEditors"/>.</summary>
        internal ObservableCollection<IAssetEditor> ObservableAssetEditors { get; } = new ObservableCollection<IAssetEditor>();

        /// <summary>The observable implementation of <see cref="AssetLoaders"/>.</summary>
        internal ObservableCollection<IAssetLoader> ObservableAssetLoaders { get; } = new ObservableCollection<IAssetLoader>();

        /// <inheritdoc />
        public IList<IAssetLoader> AssetLoaders => this.ObservableAssetLoaders;

        /// <inheritdoc />
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
            string managedAssetPrefix = contentCore.GetManagedAssetPrefix(modID);

            this.ContentCore = contentCore;
            this.GameContentManager = contentCore.CreateGameContentManager(managedAssetPrefix + ".content");
            this.ModContentManager = contentCore.CreateModContentManager(managedAssetPrefix, modName, modFolderPath, this.GameContentManager);
            this.ModName = modName;
            this.Monitor = monitor;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        [Pure]
        public string NormalizeAssetName(string assetName)
        {
            return this.ModContentManager.AssertAndNormalizeAssetName(assetName);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool InvalidateCache(string key)
        {
            string actualKey = this.GetActualAssetKey(key, ContentSource.GameContent);
            this.Monitor.Log($"Requested cache invalidation for '{actualKey}'.", LogLevel.Trace);
            return this.ContentCore.InvalidateCache(asset => asset.AssetNameEquals(actualKey)).Any();
        }

        /// <inheritdoc />
        public bool InvalidateCache<T>()
        {
            this.Monitor.Log($"Requested cache invalidation for all assets of type {typeof(T)}. This is an expensive operation and should be avoided if possible.", LogLevel.Trace);
            return this.ContentCore.InvalidateCache((key, type) => typeof(T).IsAssignableFrom(type)).Any();
        }

        /// <inheritdoc />
        public bool InvalidateCache(Func<IAssetInfo, bool> predicate)
        {
            this.Monitor.Log("Requested cache invalidation for all assets matching a predicate.", LogLevel.Trace);
            return this.ContentCore.InvalidateCache(predicate).Any();
        }

        /// <inheritdoc />
        public IAssetData GetPatchHelper<T>(T data, string assetName = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Can't get a patch helper for a null value.");

            assetName ??= $"temp/{Guid.NewGuid():N}";
            return new AssetDataForObject(this.CurrentLocale, assetName, data, this.NormalizeAssetName);
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
