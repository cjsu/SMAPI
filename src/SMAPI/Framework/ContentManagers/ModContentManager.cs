using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;
using xTile;
using xTile.Format;
using xTile.ObjectModel;
using xTile.Tiles;

namespace StardewModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files from a SMAPI mod folder with support for unpacked files.</summary>
    internal class ModContentManager : BaseContentManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;

        /// <summary>The game content manager used for map tilesheets not provided by the mod.</summary>
        private readonly IContentManager GameContentManager;

        /// <summary>The language code for language-agnostic mod assets.</summary>
        private readonly LanguageCode DefaultLanguage = Constants.DefaultLanguage;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        /// <param name="gameContentManager">The game content manager used for map tilesheets not provided by the mod.</param>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localize content.</param>
        /// <param name="coordinator">The central coordinator which manages content managers.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        /// <param name="onDisposing">A callback to invoke when the content manager is being disposed.</param>
        public ModContentManager(string name, IContentManager gameContentManager, IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, JsonHelper jsonHelper, Action<BaseContentManager> onDisposing)
            : base(name, serviceProvider, rootDirectory, currentCulture, coordinator, monitor, reflection, onDisposing, isNamespaced: true)
        {
            this.GameContentManager = gameContentManager;
            this.JsonHelper = jsonHelper;
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        public override T Load<T>(string assetName)
        {
            return this.Load<T>(assetName, this.DefaultLanguage, useCache: false);
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="language">The language code for which to load content.</param>
        public override T Load<T>(string assetName, LanguageCode language)
        {
            return this.Load<T>(assetName, language, useCache: false);
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="language">The language code for which to load content.</param>
        /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
        public override T Load<T>(string assetName, LanguageCode language, bool useCache)
        {
            assetName = this.AssertAndNormalizeAssetName(assetName);

            // disable caching
            // This is necessary to avoid assets being shared between content managers, which can
            // cause changes to an asset through one content manager affecting the same asset in
            // others (or even fresh content managers). See https://www.patreon.com/posts/27247161
            // for more background info.
            if (useCache)
                throw new InvalidOperationException("Mod content managers don't support asset caching.");

            // disable language handling
            // Mod files don't support automatic translation logic, so this should never happen.
            if (language != this.DefaultLanguage)
                throw new InvalidOperationException("Localized assets aren't supported by the mod content manager.");

            // resolve managed asset key
            {
                if (this.Coordinator.TryParseManagedAssetKey(assetName, out string contentManagerID, out string relativePath))
                {
                    if (contentManagerID != this.Name)
                        throw new SContentLoadException($"Can't load managed asset key '{assetName}' through content manager '{this.Name}' for a different mod.");
                    assetName = relativePath;
                }
            }

            // get local asset
            SContentLoadException GetContentError(string reasonPhrase) => new SContentLoadException($"Failed loading asset '{assetName}' from {this.Name}: {reasonPhrase}");
            try
            {
                // get file
                FileInfo file = this.GetModFile(assetName);
                if (!file.Exists)
                    throw GetContentError("the specified path doesn't exist.");

                // load content
                switch (file.Extension.ToLower())
                {
                    // XNB file
                    case ".xnb":
                        {
                            T data = this.RawLoad<T>(assetName, useCache: false);
                            if (data is Map map)
                            {
                                this.NormalizeTilesheetPaths(map);
                                this.FixCustomTilesheetPaths(map, relativeMapPath: assetName);
                            }
                            return data;
                        }

                    // unpacked data
                    case ".json":
                        {
                            if (!this.JsonHelper.ReadJsonFileIfExists(file.FullName, out T data))
                                throw GetContentError("the JSON file is invalid."); // should never happen since we check for file existence above
                            return data;
                        }

                    // unpacked image
                    case ".png":
                        {
                            // validate
                            if (typeof(T) != typeof(Texture2D))
                                throw GetContentError($"can't read file with extension '{file.Extension}' as type '{typeof(T)}'; must be type '{typeof(Texture2D)}'.");

                            // fetch & cache
                            using (FileStream stream = File.OpenRead(file.FullName))
                            {
                                Texture2D texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                                texture = this.PremultiplyTransparency(texture);
                                return (T)(object)texture;
                            }
                        }

                    // unpacked map
                    case ".tbin":
                        {
                            // validate
                            if (typeof(T) != typeof(Map))
                                throw GetContentError($"can't read file with extension '{file.Extension}' as type '{typeof(T)}'; must be type '{typeof(Map)}'.");

                            // fetch & cache
                            FormatManager formatManager = FormatManager.Instance;
                            Map map = formatManager.LoadMap(file.FullName);
                            this.NormalizeTilesheetPaths(map);
                            this.FixCustomTilesheetPaths(map, relativeMapPath: assetName);
                            return (T)(object)map;
                        }

                    default:
                        throw GetContentError($"unknown file extension '{file.Extension}'; must be one of '.json', '.png', '.tbin', or '.xnb'.");
                }
            }
            catch (Exception ex) when (!(ex is SContentLoadException))
            {
                if (ex.GetInnermostException() is DllNotFoundException dllEx && dllEx.Message == "libgdiplus.dylib")
                    throw GetContentError("couldn't find libgdiplus, which is needed to load mod images. Make sure Mono is installed and you're running the game through the normal launcher.");
                throw new SContentLoadException($"The content manager failed loading content asset '{assetName}' from {this.Name}.", ex);
            }
        }

        /// <summary>Create a new content manager for temporary use.</summary>
        public override LocalizedContentManager CreateTemporary()
        {
            throw new NotSupportedException("Can't create a temporary mod content manager.");
        }

        /// <summary>Get the underlying key in the game's content cache for an asset. This does not validate whether the asset exists.</summary>
        /// <param name="key">The local path to a content file relative to the mod folder.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        public string GetInternalAssetKey(string key)
        {
            FileInfo file = this.GetModFile(key);
            string relativePath = PathUtilities.GetRelativePath(this.RootDirectory, file.FullName);
            return Path.Combine(this.Name, relativePath);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether an asset has already been loaded.</summary>
        /// <param name="normalizedAssetName">The normalized asset name.</param>
        protected override bool IsNormalizedKeyLoaded(string normalizedAssetName)
        {
            return this.Cache.ContainsKey(normalizedAssetName);
        }

        /// <summary>Get a file from the mod folder.</summary>
        /// <param name="path">The asset path relative to the content folder.</param>
        private FileInfo GetModFile(string path)
        {
            // try exact match
            FileInfo file = new FileInfo(Path.Combine(this.FullRootDirectory, path));

            // try with default extension
            if (!file.Exists && file.Extension.ToLower() != ".xnb")
            {
                FileInfo result = new FileInfo(file.FullName + ".xnb");
                if (result.Exists)
                    file = result;
            }

            return file;
        }

        /// <summary>Premultiply a texture's alpha values to avoid transparency issues in the game.</summary>
        /// <param name="texture">The texture to premultiply.</param>
        /// <returns>Returns a premultiplied texture.</returns>
        /// <remarks>Based on <a href="https://gamedev.stackexchange.com/a/26037">code by David Gouveia</a>.</remarks>
        private Texture2D PremultiplyTransparency(Texture2D texture)
        {
            // Textures loaded by Texture2D.FromStream are already premultiplied on Linux/Mac, even
            // though the XNA documentation explicitly says otherwise. That's a glitch in MonoGame
            // fixed in newer versions, but the game uses a bundled version that will always be
            // affected. See https://github.com/MonoGame/MonoGame/issues/4820 for more info.
            if (Constants.TargetPlatform != GamePlatform.Windows)
                return texture;

            // premultiply pixels
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            for (int i = 0; i < data.Length; i++)
                data[i] = Color.FromNonPremultiplied(data[i].ToVector4());
            texture.SetData(data);
            return texture;
        }

        /// <summary>Normalize map tilesheet paths for the current platform.</summary>
        /// <param name="map">The map whose tilesheets to fix.</param>
        private void NormalizeTilesheetPaths(Map map)
        {
            foreach (TileSheet tilesheet in map.TileSheets)
                tilesheet.ImageSource = this.NormalizePathSeparators(tilesheet.ImageSource);
        }

        /// <summary>Fix custom map tilesheet paths so they can be found by the content manager.</summary>
        /// <param name="map">The map whose tilesheets to fix.</param>
        /// <param name="relativeMapPath">The relative map path within the mod folder.</param>
        /// <exception cref="ContentLoadException">A map tilesheet couldn't be resolved.</exception>
        /// <remarks>
        /// The game's logic for tilesheets in <see cref="Game1.setGraphicsForSeason"/> is a bit specialized. It boils
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
            relativeMapPath = this.AssertAndNormalizeAssetName(relativeMapPath); // Mono's Path.GetDirectoryName doesn't handle Windows dir separators
            string relativeMapFolder = Path.GetDirectoryName(relativeMapPath) ?? ""; // folder path containing the map, relative to the mod folder
            bool isOutdoors = map.Properties.TryGetValue("Outdoors", out PropertyValue outdoorsProperty) && outdoorsProperty != null;

            // fix tilesheets
            foreach (TileSheet tilesheet in map.TileSheets)
            {
                string imageSource = tilesheet.ImageSource;

                // validate tilesheet path
                if (Path.IsPathRooted(imageSource) || PathUtilities.GetSegments(imageSource).Contains(".."))
                    throw new ContentLoadException($"The '{imageSource}' tilesheet couldn't be loaded. Tilesheet paths must be a relative path without directory climbing (../).");

                // get seasonal name (if applicable)
                string seasonalImageSource = null;
                if (isOutdoors && Context.IsSaveLoaded && Game1.currentSeason != null)
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
                    return this.GetInternalAssetKey(localKey);
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
                        this.GameContentManager.Load<Texture2D>(contentKey, this.Language, useCache: true); // no need to bypass cache here, since we're not storing the asset
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
                        if (this.GetContentFolderFileExists(contentKey))
                            throw;
                    }
                }
            }

            // not found
            return null;
        }

        /// <summary>Get whether a file from the game's content folder exists.</summary>
        /// <param name="key">The asset key.</param>
        private bool GetContentFolderFileExists(string key)
        {
            // get file path
            string path = Path.Combine(this.GameContentManager.FullRootDirectory, key);
            if (!path.EndsWith(".xnb"))
                path += ".xnb";

            // get file
            return new FileInfo(path).Exists;
        }
    }
}
