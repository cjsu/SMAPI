using System;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewValley;

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


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localise content.</param>
        /// <param name="coordinator">The central coordinator which manages content managers.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        /// <param name="onDisposing">A callback to invoke when the content manager is being disposed.</param>
        public ModContentManager(string name, IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, JsonHelper jsonHelper, Action<BaseContentManager> onDisposing)
            : base(name, serviceProvider, rootDirectory, currentCulture, coordinator, monitor, reflection, onDisposing, isModFolder: true)
        {
            this.JsonHelper = jsonHelper;
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="language">The language code for which to load content.</param>
        public override T Load<T>(string assetName, LanguageCode language)
        {
            assetName = this.AssertAndNormaliseAssetName(assetName);

            // get from cache
            if (this.IsLoaded(assetName))
                return base.Load<T>(assetName, language);

            // get managed asset
            if (this.Coordinator.TryParseManagedAssetKey(assetName, out string contentManagerID, out string relativePath))
            {
                if (contentManagerID != this.Name)
                {
                    T data = this.Coordinator.LoadAndCloneManagedAsset<T>(assetName, contentManagerID, relativePath, language);
                    this.Inject(assetName, data);
                    return data;
                }

                return this.LoadManagedAsset<T>(assetName, contentManagerID, relativePath, language);
            }

            throw new NotSupportedException("Can't load content folder asset from a mod content manager.");
        }

        /// <summary>Create a new content manager for temporary use.</summary>
        public override LocalizedContentManager CreateTemporary()
        {
            throw new NotSupportedException("Can't create a temporary mod content manager.");
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether an asset has already been loaded.</summary>
        /// <param name="normalisedAssetName">The normalised asset name.</param>
        protected override bool IsNormalisedKeyLoaded(string normalisedAssetName)
        {
            return this.Cache.ContainsKey(normalisedAssetName);
        }

        /// <summary>Load a managed mod asset.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="internalKey">The internal asset key.</param>
        /// <param name="contentManagerID">The unique name for the content manager which should load this asset.</param>
        /// <param name="relativePath">The relative path within the mod folder.</param>
        /// <param name="language">The language code for which to load content.</param>
        private T LoadManagedAsset<T>(string internalKey, string contentManagerID, string relativePath, LanguageCode language)
        {
            SContentLoadException GetContentError(string reasonPhrase) => new SContentLoadException($"Failed loading asset '{relativePath}' from {contentManagerID}: {reasonPhrase}");
            try
            {
                // get file
                FileInfo file = this.GetModFile(relativePath);
                if (!file.Exists)
                    throw GetContentError("the specified path doesn't exist.");

                // load content
                switch (file.Extension.ToLower())
                {
                    // XNB file
                    case ".xnb":
                        return base.Load<T>(relativePath, language);

                    // unpacked data
                    case ".json":
                        {
                            if (!this.JsonHelper.ReadJsonFileIfExists(file.FullName, out T data))
                                throw GetContentError("the JSON file is invalid."); // should never happen since we check for file existence above

                            return data;
                        }

                    // unpacked image
                    case ".png":
                        // validate
                        if (typeof(T) != typeof(Texture2D))
                            throw GetContentError($"can't read file with extension '{file.Extension}' as type '{typeof(T)}'; must be type '{typeof(Texture2D)}'.");

                        // fetch & cache
                        using (FileStream stream = File.OpenRead(file.FullName))
                        {
                            Texture2D texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                            texture = this.PremultiplyTransparency(texture);
                            this.Inject(internalKey, texture);
                            return (T)(object)texture;
                        }

                    // unpacked map
                    case ".tbin":
                        throw GetContentError($"can't read unpacked map file directly from the underlying content manager. It must be loaded through the mod's {typeof(IModHelper)}.{nameof(IModHelper.Content)} helper.");

                    default:
                        throw GetContentError($"unknown file extension '{file.Extension}'; must be one of '.png', '.tbin', or '.xnb'.");
                }
            }
            catch (Exception ex) when (!(ex is SContentLoadException))
            {
                if (ex.GetInnermostException() is DllNotFoundException dllEx && dllEx.Message == "libgdiplus.dylib")
                    throw GetContentError("couldn't find libgdiplus, which is needed to load mod images. Make sure Mono is installed and you're running the game through the normal launcher.");
                throw new SContentLoadException($"The content manager failed loading content asset '{relativePath}' from {contentManagerID}.", ex);
            }
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
    }
}
