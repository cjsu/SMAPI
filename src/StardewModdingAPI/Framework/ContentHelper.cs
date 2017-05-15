using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using xTile;
using xTile.Format;
using xTile.Tiles;

namespace StardewModdingAPI.Framework
{
    /// <summary>Provides an API for loading content assets.</summary>
    internal class ContentHelper : IContentHelper
    {
        /*********
        ** Properties
        *********/
        /// <summary>SMAPI's underlying content manager.</summary>
        private readonly SContentManager ContentManager;

        /// <summary>The absolute path to the mod folder.</summary>
        private readonly string ModFolderPath;

        /// <summary>The path to the mod's folder, relative to the game's content folder (e.g. "../Mods/ModName").</summary>
        private readonly string ModFolderPathFromContent;

        /// <summary>The friendly mod name for use in errors.</summary>
        private readonly string ModName;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="contentManager">SMAPI's underlying content manager.</param>
        /// <param name="modFolderPath">The absolute path to the mod folder.</param>
        /// <param name="modName">The friendly mod name for use in errors.</param>
        public ContentHelper(SContentManager contentManager, string modFolderPath, string modName)
        {
            this.ContentManager = contentManager;
            this.ModFolderPath = modFolderPath;
            this.ModName = modName;
            this.ModFolderPathFromContent = this.GetRelativePath(contentManager.FullRootDirectory, modFolderPath);
        }

        /// <summary>Load content from the game folder or mod folder (if not already cached), and return it. When loading a <c>.png</c> file, this must be called outside the game's draw loop.</summary>
        /// <typeparam name="T">The expected data type. The main supported types are <see cref="Texture2D"/> and dictionaries; other types may be supported by the game's content pipeline.</typeparam>
        /// <param name="key">The asset key to fetch (if the <paramref name="source"/> is <see cref="ContentSource.GameContent"/>), or the local path to a content file relative to the mod folder.</param>
        /// <param name="source">Where to search for a matching content asset.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        /// <exception cref="ContentLoadException">The content asset couldn't be loaded (e.g. because it doesn't exist).</exception>
        public T Load<T>(string key, ContentSource source = ContentSource.ModFolder)
        {
            this.AssertValidAssetKeyFormat(key);
            try
            {
                switch (source)
                {
                    case ContentSource.GameContent:
                        return this.ContentManager.Load<T>(key);

                    case ContentSource.ModFolder:
                        // get file
                        FileInfo file = this.GetModFile(key);
                        if (!file.Exists)
                            throw new ContentLoadException($"There is no file at path '{file.FullName}'.");

                        // get asset path
                        string assetPath = this.GetModAssetPath(key, file.FullName);

                        // load content
                        switch (file.Extension.ToLower())
                        {
                            case ".xnb":
                                return this.ContentManager.Load<T>(assetPath);

                            case ".tbin":
                                // validate
                                if (typeof(T) != typeof(Map))
                                    throw new ContentLoadException($"Can't read file with extension '{file.Extension}' as type '{typeof(T)}'; must be type '{typeof(Map)}'.");
                                
                                // try cache
                                if (this.ContentManager.IsLoaded(assetPath))
                                    return this.ContentManager.Load<T>(assetPath);

                                // fetch & cache
                                FormatManager formatManager = FormatManager.Instance;
                                Map map = formatManager.LoadMap(file.FullName);
                                foreach (TileSheet t in map.TileSheets)
                                    t.ImageSource = t.ImageSource.Replace(".png", "");
                                this.ContentManager.Inject(assetPath, map);
                                return (T)(object)map;
                            
                            case ".png":
                                // validate
                                if (typeof(T) != typeof(Texture2D))
                                    throw new ContentLoadException($"Can't read file with extension '{file.Extension}' as type '{typeof(T)}'; must be type '{typeof(Texture2D)}'.");

                                // try cache
                                if (this.ContentManager.IsLoaded(assetPath))
                                    return this.ContentManager.Load<T>(assetPath);

                                // fetch & cache
                                using (FileStream stream = File.OpenRead(file.FullName))
                                {
                                    Texture2D texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                                    texture = this.PremultiplyTransparency(texture);
                                    this.ContentManager.Inject(assetPath, texture);
                                    return (T)(object)texture;
                                }
                                

                            


                            default:
                                throw new ContentLoadException($"Unknown file extension '{file.Extension}'; must be '.xnb' or '.png'.");
                        }

                    default:
                        throw new NotSupportedException($"Unknown content source '{source}'.");
                }
            }
            catch (Exception ex)
            {
                throw new ContentLoadException($"{this.ModName} failed loading content asset '{key}' from {source}.", ex);
            }
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
                    return this.ContentManager.NormaliseAssetName(key);

                case ContentSource.ModFolder:
                    FileInfo file = this.GetModFile(key);
                    return this.ContentManager.NormaliseAssetName(this.GetModAssetPath(key, file.FullName));

                default:
                    throw new NotSupportedException($"Unknown content source '{source}'.");
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Assert that the given key has a valid format.</summary>
        /// <param name="key">The asset key to check.</param>
        /// <exception cref="ArgumentException">The asset key is empty or contains invalid characters.</exception>
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Parameter is only used for assertion checks by design.")]
        private void AssertValidAssetKeyFormat(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("The asset key or local path is empty.");
            if (key.Intersect(Path.GetInvalidPathChars()).Any())
                throw new ArgumentException("The asset key or local path contains invalid characters.");
        }

        /// <summary>Get a file from the mod folder.</summary>
        /// <param name="path">The asset path relative to the mod folder.</param>
        private FileInfo GetModFile(string path)
        {
            path = Path.Combine(this.ModFolderPath, this.ContentManager.NormalisePathSeparators(path));
            FileInfo file = new FileInfo(path);
            if (!file.Exists && file.Extension == "")
                file = new FileInfo(Path.Combine(this.ModFolderPath, path + ".xnb"));
            return file;
        }

        /// <summary>Get the asset path which loads a mod folder through a content manager.</summary>
        /// <param name="localPath">The file path relative to the mod's folder.</param>
        /// <param name="absolutePath">The absolute file path.</param>
        private string GetModAssetPath(string localPath, string absolutePath)
        {
#if SMAPI_FOR_WINDOWS
            // XNA doesn't allow absolute asset paths, so get a path relative to the content folder
            return Path.Combine(this.ModFolderPathFromContent, localPath);
#else
            // MonoGame is weird about relative paths on Mac, but allows absolute paths
            return absolutePath;
#endif
        }

        /// <summary>Get a directory path relative to a given root.</summary>
        /// <param name="rootPath">The root path from which the path should be relative.</param>
        /// <param name="targetPath">The target file path.</param>
        private string GetRelativePath(string rootPath, string targetPath)
        {
            // convert to URIs
            Uri from = new Uri(rootPath + "/");
            Uri to = new Uri(targetPath + "/");
            if (from.Scheme != to.Scheme)
                throw new InvalidOperationException($"Can't get path for '{targetPath}' relative to '{rootPath}'.");

            // get relative path
            return Uri.UnescapeDataString(from.MakeRelativeUri(to).ToString())
                .Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar); // use correct separator for platform
        }

        /// <summary>Premultiply a texture's alpha values to avoid transparency issues in the game. This is only possible if the game isn't currently drawing.</summary>
        /// <param name="texture">The texture to premultiply.</param>
        /// <returns>Returns a premultiplied texture.</returns>
        /// <remarks>Based on <a href="https://gist.github.com/Layoric/6255384">code by Layoric</a>.</remarks>
        private Texture2D PremultiplyTransparency(Texture2D texture)
        {
            // validate
            if (Context.IsInDrawLoop)
                throw new NotSupportedException("Can't load a PNG file while the game is drawing to the screen. Make sure you load content outside the draw loop.");

            // process texture
            SpriteBatch spriteBatch = Game1.spriteBatch;
            GraphicsDevice gpu = Game1.graphics.GraphicsDevice;
            using (RenderTarget2D renderTarget = new RenderTarget2D(Game1.graphics.GraphicsDevice, texture.Width, texture.Height))
            {
                // create blank render target to premultiply
                gpu.SetRenderTarget(renderTarget);
                gpu.Clear(Color.Black);

                // multiply each color by the source alpha, and write just the color values into the final texture
                spriteBatch.Begin(SpriteSortMode.Immediate, new BlendState
                {
                    ColorDestinationBlend = Blend.Zero,
                    ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue,
                    AlphaDestinationBlend = Blend.Zero,
                    AlphaSourceBlend = Blend.SourceAlpha,
                    ColorSourceBlend = Blend.SourceAlpha
                });
                spriteBatch.Draw(texture, texture.Bounds, Color.White);
                spriteBatch.End();

                // copy the alpha values from the source texture into the final one without multiplying them
                spriteBatch.Begin(SpriteSortMode.Immediate, new BlendState
                {
                    ColorWriteChannels = ColorWriteChannels.Alpha,
                    AlphaDestinationBlend = Blend.Zero,
                    ColorDestinationBlend = Blend.Zero,
                    AlphaSourceBlend = Blend.One,
                    ColorSourceBlend = Blend.One
                });
                spriteBatch.Draw(texture, texture.Bounds, Color.White);
                spriteBatch.End();

                // release GPU
                gpu.SetRenderTarget(null);

                // extract premultiplied data
                Color[] data = new Color[texture.Width * texture.Height];
                renderTarget.GetData(data);

                // unset texture from GPU to regain control
                gpu.Textures[0] = null;

                // update texture with premultiplied data
                texture.SetData(data);
            }

            return texture;
        }
    }
}
