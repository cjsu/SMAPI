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
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;
using xTile;
using xTile.Format;
using xTile.Tiles;

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
        private readonly IContentManager ModContentManager;

        /// <summary>The absolute path to the mod folder.</summary>
        private readonly string ModFolderPath;

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
            this.ModContentManager = contentCore.CreateModContentManager(this.ContentCore.GetManagedAssetPrefix(modID), rootDirectory: modFolderPath);
            this.ModFolderPath = modFolderPath;
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
            SContentLoadException GetContentError(string reasonPhrase) => new SContentLoadException($"{this.ModName} failed loading content asset '{key}' from {source}: {reasonPhrase}.");

            try
            {
                this.AssertAndNormaliseAssetName(key);
                switch (source)
                {
                    case ContentSource.GameContent:
                        return this.GameContentManager.Load<T>(key);

                    case ContentSource.ModFolder:
                        // get file
                        FileInfo file = this.GetModFile(key);
                        if (!file.Exists)
                            throw GetContentError($"there's no matching file at path '{file.FullName}'.");
                        string internalKey = this.GetInternalModAssetKey(file);

                        // try cache
                        if (this.ModContentManager.IsLoaded(internalKey))
                            return this.ModContentManager.Load<T>(internalKey);

                        // fix map tilesheets
                        if (file.Extension.ToLower() == ".tbin")
                        {
                            // validate
                            if (typeof(T) != typeof(Map))
                                throw GetContentError($"can't read file with extension '{file.Extension}' as type '{typeof(T)}'; must be type '{typeof(Map)}'.");

                            // fetch & cache
                            FormatManager formatManager = FormatManager.Instance;
                            Map map = formatManager.LoadMap(file.FullName);
                            this.FixCustomTilesheetPaths(map, relativeMapPath: key);

                            // inject map
                            this.ModContentManager.Inject(internalKey, map, this.CurrentLocaleConstant);
                            return (T)(object)map;
                        }

                        // load through content manager
                        return this.ModContentManager.Load<T>(internalKey);

                    default:
                        throw GetContentError($"unknown content source '{source}'.");
                }
            }
            catch (Exception ex) when (!(ex is SContentLoadException))
            {
                throw new SContentLoadException($"{this.ModName} failed loading content asset '{key}' from {source}.", ex);
            }
        }

        /// <summary>Normalise an asset name so it's consistent with those generated by the game. This is mainly useful for string comparisons like <see cref="string.StartsWith(string)"/> on generated asset names, and isn't necessary when passing asset names into other content helper methods.</summary>
        /// <param name="assetName">The asset key.</param>
        [Pure]
        public string NormaliseAssetName(string assetName)
        {
            return this.ModContentManager.AssertAndNormaliseAssetName(assetName);
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
                    return this.GameContentManager.AssertAndNormaliseAssetName(key);

                case ContentSource.ModFolder:
                    FileInfo file = this.GetModFile(key);
                    return this.GetInternalModAssetKey(file);

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
        private void AssertAndNormaliseAssetName(string key)
        {
            this.ModContentManager.AssertAndNormaliseAssetName(key);
            if (Path.IsPathRooted(key))
                throw new ArgumentException("The asset key must not be an absolute path.");
        }

        /// <summary>Get the internal key in the content cache for a mod asset.</summary>
        /// <param name="modFile">The asset file.</param>
        private string GetInternalModAssetKey(FileInfo modFile)
        {
            string relativePath = PathUtilities.GetRelativePath(this.ModFolderPath, modFile.FullName);
            return Path.Combine(this.ModContentManager.Name, relativePath);
        }

        /// <summary>Fix custom map tilesheet paths so they can be found by the content manager.</summary>
        /// <param name="map">The map whose tilesheets to fix.</param>
        /// <param name="relativeMapPath">The relative map path within the mod folder.</param>
        /// <exception cref="ContentLoadException">A map tilesheet couldn't be resolved.</exception>
        /// <remarks>
        /// The game's logic for tilesheets in <see cref="Game1.setGraphicsForSeason"/> is a bit specialised. It boils
        /// down to this:
        ///  * If the location is indoors or the desert, or the image source contains 'path' or 'object', it's loaded
        ///    as-is relative to the <c>Content</c> folder.
        ///  * Else it's loaded from <c>Content\Maps</c> with a seasonal prefix.
        /// 
        /// That logic doesn't work well in our case, mainly because we have no location metadata at this point.
        /// Instead we use a more heuristic approach: check relative to the map file first, then relative to
        /// <c>Content\Maps</c>, then <c>Content</c>. If the image source filename contains a seasonal prefix, try for a
        /// seasonal variation and then an exact match.
        /// 
        /// While that doesn't exactly match the game logic, it's close enough that it's unlikely to make a difference.
        /// </remarks>
        private void FixCustomTilesheetPaths(Map map, string relativeMapPath)
        {
            // get map info
            if (!map.TileSheets.Any())
                return;
            relativeMapPath = this.ModContentManager.AssertAndNormaliseAssetName(relativeMapPath); // Mono's Path.GetDirectoryName doesn't handle Windows dir separators
            string relativeMapFolder = Path.GetDirectoryName(relativeMapPath) ?? ""; // folder path containing the map, relative to the mod folder

            // fix tilesheets
            foreach (TileSheet tilesheet in map.TileSheets)
            {
                string imageSource = tilesheet.ImageSource;

                // validate tilesheet path
                if (Path.IsPathRooted(imageSource) || PathUtilities.GetSegments(imageSource).Contains(".."))
                    throw new ContentLoadException($"The '{imageSource}' tilesheet couldn't be loaded. Tilesheet paths must be a relative path without directory climbing (../).");

                // get seasonal name (if applicable)
                string seasonalImageSource = null;
                if (Context.IsSaveLoaded && Game1.currentSeason != null)
                {
                    string filename = Path.GetFileName(imageSource) ?? throw new InvalidOperationException($"The '{imageSource}' tilesheet couldn't be loaded: filename is unexpectedly null.");
                    bool hasSeasonalPrefix =
                        filename.StartsWith("spring_", StringComparison.CurrentCultureIgnoreCase)
                        || filename.StartsWith("summer_", StringComparison.CurrentCultureIgnoreCase)
                        || filename.StartsWith("fall_", StringComparison.CurrentCultureIgnoreCase)
                        || filename.StartsWith("winter_", StringComparison.CurrentCultureIgnoreCase);
                    if (hasSeasonalPrefix && !filename.StartsWith(Game1.currentSeason + "_"))
                    {
                        string dirPath = imageSource.Substring(0, imageSource.LastIndexOf(filename, StringComparison.CurrentCultureIgnoreCase));
                        seasonalImageSource = $"{dirPath}{Game1.currentSeason}_{filename.Substring(filename.IndexOf("_", StringComparison.CurrentCultureIgnoreCase) + 1)}";
                    }
                }

                // load best match
                try
                {
                    string key =
                        this.GetTilesheetAssetName(relativeMapFolder, seasonalImageSource)
                        ?? this.GetTilesheetAssetName(relativeMapFolder, imageSource);
                    if (key != null)
                    {
                        tilesheet.ImageSource = key;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    throw new ContentLoadException($"The '{imageSource}' tilesheet couldn't be loaded relative to either map file or the game's content folder.", ex);
                }

                // none found
                throw new ContentLoadException($"The '{imageSource}' tilesheet couldn't be loaded relative to either map file or the game's content folder.");
            }
        }

        /// <summary>Get the actual asset name for a tilesheet.</summary>
        /// <param name="modRelativeMapFolder">The folder path containing the map, relative to the mod folder.</param>
        /// <param name="imageSource">The tilesheet image source to load.</param>
        /// <returns>Returns the asset name.</returns>
        /// <remarks>See remarks on <see cref="FixCustomTilesheetPaths"/>.</remarks>
        private string GetTilesheetAssetName(string modRelativeMapFolder, string imageSource)
        {
            if (imageSource == null)
                return null;

            // check relative to map file
            {
                string localKey = Path.Combine(modRelativeMapFolder, imageSource);
                FileInfo localFile = this.GetModFile(localKey);
                if (localFile.Exists)
                    return this.GetActualAssetKey(localKey);
            }

            // check relative to content folder
            {
                foreach (string candidateKey in new[] { imageSource, Path.Combine("Maps", imageSource) })
                {
                    string contentKey = candidateKey.EndsWith(".png")
                        ? candidateKey.Substring(0, candidateKey.Length - 4)
                        : candidateKey;

                    try
                    {
                        this.Load<Texture2D>(contentKey, ContentSource.GameContent);
                        return contentKey;
                    }
                    catch
                    {
                        // ignore file-not-found errors
                        // TODO: while it's useful to suppress an asset-not-found error here to avoid
                        // confusion, this is a pretty naive approach. Even if the file doesn't exist,
                        // the file may have been loaded through an IAssetLoader which failed. So even
                        // if the content file doesn't exist, that doesn't mean the error here is a
                        // content-not-found error. Unfortunately XNA doesn't provide a good way to
                        // detect the error type.
                        if (this.GetContentFolderFile(contentKey).Exists)
                            throw;
                    }
                }
            }

            // not found
            return null;
        }

        /// <summary>Get a file from the mod folder.</summary>
        /// <param name="path">The asset path relative to the mod folder.</param>
        private FileInfo GetModFile(string path)
        {
            // try exact match
            path = Path.Combine(this.ModFolderPath, this.ModContentManager.NormalisePathSeparators(path));
            FileInfo file = new FileInfo(path);

            // try with default extension
            if (!file.Exists && file.Extension.ToLower() != ".xnb")
            {
                FileInfo result = new FileInfo(path + ".xnb");
                if (result.Exists)
                    file = result;
            }

            return file;
        }

        /// <summary>Get a file from the game's content folder.</summary>
        /// <param name="key">The asset key.</param>
        private FileInfo GetContentFolderFile(string key)
        {
            // get file path
            string path = Path.Combine(this.GameContentManager.FullRootDirectory, key);
            if (!path.EndsWith(".xnb"))
                path += ".xnb";

            // get file
            return new FileInfo(path);
        }
    }
}
