using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.AssemblyRewriters;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Objects;
using StardewValley.Projectiles;

namespace StardewModdingAPI.Framework
{
    /// <summary>SMAPI's implementation of the game's content manager which lets it raise content events.</summary>
    internal class SContentManager : LocalizedContentManager
    {
        /*********
        ** Properties
        *********/
        /// <summary>The possible directory separator characters in an asset key.</summary>
        private static readonly char[] PossiblePathSeparators = new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.Distinct().ToArray();

        /// <summary>The preferred directory separator chaeacter in an asset key.</summary>
        private static readonly string PreferredPathSeparator = Path.DirectorySeparatorChar.ToString();

        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The underlying content manager's asset cache.</summary>
        private readonly IDictionary<string, object> Cache;

        /// <summary>Applies platform-specific asset key normalisation so it's consistent with the underlying cache.</summary>
        private readonly Func<string, string> NormaliseAssetNameForPlatform;

        /// <summary>The private <see cref="LocalizedContentManager"/> method which generates the locale portion of an asset name.</summary>
        private readonly IPrivateMethod GetKeyLocale;


        /*********
        ** Accessors
        *********/
        /// <summary>Interceptors which provide the initial versions of matching assets.</summary>
        internal IDictionary<IModMetadata, IList<IAssetLoader>> Loaders { get; } = new Dictionary<IModMetadata, IList<IAssetLoader>>();

        /// <summary>Interceptors which edit matching assets after they're loaded.</summary>
        internal IDictionary<IModMetadata, IList<IAssetEditor>> Editors { get; } = new Dictionary<IModMetadata, IList<IAssetEditor>>();

        /// <summary>The absolute path to the <see cref="ContentManager.RootDirectory"/>.</summary>
        public string FullRootDirectory => Path.Combine(Constants.ExecutionPath, this.RootDirectory);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localise content.</param>
        /// <param name="languageCodeOverride">The current language code for which to localise content.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public SContentManager(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, string languageCodeOverride, IMonitor monitor)
            : base(serviceProvider, rootDirectory, currentCulture, languageCodeOverride)
        {
            // validate
            if (monitor == null)
                throw new ArgumentNullException(nameof(monitor));

            // initialise
            var reflection = new Reflector();
            this.Monitor = monitor;

            // get underlying fields for interception
            this.Cache = reflection.GetPrivateField<Dictionary<string, object>>(this, "loadedAssets").GetValue();
            this.GetKeyLocale = reflection.GetPrivateMethod(this, "languageCode");

            // get asset key normalisation logic
            if (Constants.TargetPlatform == Platform.Windows)
            {
                IPrivateMethod method = reflection.GetPrivateMethod(typeof(TitleContainer), "GetCleanPath");
                this.NormaliseAssetNameForPlatform = path => method.Invoke<string>(path);
            }
            else
                this.NormaliseAssetNameForPlatform = key => key.Replace('\\', '/'); // based on MonoGame's ContentManager.Load<T> logic
        }

        /// <summary>Normalise path separators in a file path. For asset keys, see <see cref="NormaliseAssetName"/> instead.</summary>
        /// <param name="path">The file path to normalise.</param>
        public string NormalisePathSeparators(string path)
        {
            string[] parts = path.Split(SContentManager.PossiblePathSeparators, StringSplitOptions.RemoveEmptyEntries);
            string normalised = string.Join(SContentManager.PreferredPathSeparator, parts);
            if (path.StartsWith(SContentManager.PreferredPathSeparator))
                normalised = SContentManager.PreferredPathSeparator + normalised; // keep root slash
            return normalised;
        }

        /// <summary>Normalise an asset name so it's consistent with the underlying cache.</summary>
        /// <param name="assetName">The asset key.</param>
        public string NormaliseAssetName(string assetName)
        {
            assetName = this.NormalisePathSeparators(assetName);
            if (assetName.EndsWith(".xnb", StringComparison.InvariantCultureIgnoreCase))
                return assetName.Substring(0, assetName.Length - 4);
            return this.NormaliseAssetNameForPlatform(assetName);
        }

        /// <summary>Get whether the content manager has already loaded and cached the given asset.</summary>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        public bool IsLoaded(string assetName)
        {
            assetName = this.NormaliseAssetName(assetName);
            return this.IsNormalisedKeyLoaded(assetName);
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        public override T Load<T>(string assetName)
        {
            assetName = this.NormaliseAssetName(assetName);

            // skip if already loaded
            if (this.IsNormalisedKeyLoaded(assetName))
                return base.Load<T>(assetName);

            // load asset
            T data;
            {
                IAssetInfo info = new AssetInfo(this.GetLocale(), assetName, typeof(T), this.NormaliseAssetName);
                IAssetData asset = this.ApplyLoader<T>(info) ?? new AssetDataForObject(info, base.Load<T>(assetName), this.NormaliseAssetName);
                asset = this.ApplyEditors<T>(info, asset);
                data = (T)asset.Data;
            }

            // update cache & return data
            this.Cache[assetName] = data;
            return data;
        }

        /// <summary>Inject an asset into the cache.</summary>
        /// <typeparam name="T">The type of asset to inject.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="value">The asset value.</param>
        public void Inject<T>(string assetName, T value)
        {
            assetName = this.NormaliseAssetName(assetName);

            this.Cache[assetName] = value;
        }

        /// <summary>Get the current content locale.</summary>
        public string GetLocale()
        {
            return this.GetKeyLocale.Invoke<string>();
        }

        /// <summary>Reset the asset cache and reload the game's static assets.</summary>
        /// <remarks>This implementation is derived from <see cref="Game1.LoadContent"/>.</remarks>
        public void Reset()
        {
            this.Monitor.Log("Resetting asset cache...", LogLevel.Trace);
            this.Cache.Clear();

            // from Game1.LoadContent
            Game1.daybg = this.Load<Texture2D>("LooseSprites\\daybg");
            Game1.nightbg = this.Load<Texture2D>("LooseSprites\\nightbg");
            Game1.menuTexture = this.Load<Texture2D>("Maps\\MenuTiles");
            Game1.lantern = this.Load<Texture2D>("LooseSprites\\Lighting\\lantern");
            Game1.windowLight = this.Load<Texture2D>("LooseSprites\\Lighting\\windowLight");
            Game1.sconceLight = this.Load<Texture2D>("LooseSprites\\Lighting\\sconceLight");
            Game1.cauldronLight = this.Load<Texture2D>("LooseSprites\\Lighting\\greenLight");
            Game1.indoorWindowLight = this.Load<Texture2D>("LooseSprites\\Lighting\\indoorWindowLight");
            Game1.shadowTexture = this.Load<Texture2D>("LooseSprites\\shadow");
            Game1.mouseCursors = this.Load<Texture2D>("LooseSprites\\Cursors");
            Game1.controllerMaps = this.Load<Texture2D>("LooseSprites\\ControllerMaps");
            Game1.animations = this.Load<Texture2D>("TileSheets\\animations");
            Game1.achievements = this.Load<Dictionary<int, string>>("Data\\Achievements");
            Game1.NPCGiftTastes = this.Load<Dictionary<string, string>>("Data\\NPCGiftTastes");
            Game1.dialogueFont = this.Load<SpriteFont>("Fonts\\SpriteFont1");
            Game1.smallFont = this.Load<SpriteFont>("Fonts\\SmallFont");
            Game1.tinyFont = this.Load<SpriteFont>("Fonts\\tinyFont");
            Game1.tinyFontBorder = this.Load<SpriteFont>("Fonts\\tinyFontBorder");
            Game1.objectSpriteSheet = this.Load<Texture2D>("Maps\\springobjects");
            Game1.cropSpriteSheet = this.Load<Texture2D>("TileSheets\\crops");
            Game1.emoteSpriteSheet = this.Load<Texture2D>("TileSheets\\emotes");
            Game1.debrisSpriteSheet = this.Load<Texture2D>("TileSheets\\debris");
            Game1.bigCraftableSpriteSheet = this.Load<Texture2D>("TileSheets\\Craftables");
            Game1.rainTexture = this.Load<Texture2D>("TileSheets\\rain");
            Game1.buffsIcons = this.Load<Texture2D>("TileSheets\\BuffsIcons");
            Game1.objectInformation = this.Load<Dictionary<int, string>>("Data\\ObjectInformation");
            Game1.bigCraftablesInformation = this.Load<Dictionary<int, string>>("Data\\BigCraftablesInformation");
            FarmerRenderer.hairStylesTexture = this.Load<Texture2D>("Characters\\Farmer\\hairstyles");
            FarmerRenderer.shirtsTexture = this.Load<Texture2D>("Characters\\Farmer\\shirts");
            FarmerRenderer.hatsTexture = this.Load<Texture2D>("Characters\\Farmer\\hats");
            FarmerRenderer.accessoriesTexture = this.Load<Texture2D>("Characters\\Farmer\\accessories");
            Furniture.furnitureTexture = this.Load<Texture2D>("TileSheets\\furniture");
            SpriteText.spriteTexture = this.Load<Texture2D>("LooseSprites\\font_bold");
            SpriteText.coloredTexture = this.Load<Texture2D>("LooseSprites\\font_colored");
            Tool.weaponsTexture = this.Load<Texture2D>("TileSheets\\weapons");
            Projectile.projectileSheet = this.Load<Texture2D>("TileSheets\\Projectiles");

            // from Farmer constructor
            if (Game1.player != null)
                Game1.player.FarmerRenderer = new FarmerRenderer(this.Load<Texture2D>("Characters\\Farmer\\farmer_" + (Game1.player.isMale ? "" : "girl_") + "base"));
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether an asset has already been loaded.</summary>
        /// <param name="normalisedAssetName">The normalised asset name.</param>
        private bool IsNormalisedKeyLoaded(string normalisedAssetName)
        {
            return this.Cache.ContainsKey(normalisedAssetName)
                || this.Cache.ContainsKey($"{normalisedAssetName}.{this.GetKeyLocale.Invoke<string>()}"); // translated asset
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
                        this.Monitor.Log($"{entry.Key.DisplayName} crashed when checking whether it could load asset '{info.AssetName}', and will be ignored. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
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
                this.Monitor.Log($"{mod.DisplayName} crashed when loading asset '{info.AssetName}'. SMAPI will use the default asset instead. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                return null;
            }

            // validate asset
            if (data == null)
            {
                this.Monitor.Log($"{mod.DisplayName} incorrectly set asset '{info.AssetName}' to a null value; ignoring override.", LogLevel.Error);
                return null;
            }

            // return matched asset
            return new AssetDataForObject(info, data, this.NormaliseAssetName);
        }

        /// <summary>Apply any <see cref="Editors"/> to a loaded asset.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="info">The basic asset metadata.</param>
        /// <param name="asset">The loaded asset.</param>
        private IAssetData ApplyEditors<T>(IAssetInfo info, IAssetData asset)
        {
            IAssetData GetNewData(object data) => new AssetDataForObject(info, data, this.NormaliseAssetName);

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
                    this.Monitor.Log($"{mod.DisplayName} crashed when checking whether it could edit asset '{info.AssetName}', and will be ignored. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                    continue;
                }

                // try edit
                object prevAsset = asset.Data;
                try
                {
                    editor.Edit<T>(asset);
                    this.Monitor.Log($"{mod.DisplayName} intercepted {info.AssetName}.", LogLevel.Trace);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"{mod.DisplayName} crashed when editing asset '{info.AssetName}', which may cause errors in-game. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }

                // validate edit
                if (asset.Data == null)
                {
                    this.Monitor.Log($"{mod.DisplayName} incorrectly set asset '{info.AssetName}' to a null value; ignoring override.", LogLevel.Warn);
                    asset = GetNewData(prevAsset);
                }
                else if (!(asset.Data is T))
                {
                    this.Monitor.Log($"{mod.DisplayName} incorrectly set asset '{asset.AssetName}' to incompatible type '{asset.Data.GetType()}', expected '{typeof(T)}'; ignoring override.", LogLevel.Warn);
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
                IModMetadata metadata = entry.Key;
                IList<T> interceptors = entry.Value;

                // special case if mod is an interceptor
                if (metadata.Mod is T modAsInterceptor)
                    yield return new KeyValuePair<IModMetadata, T>(metadata, modAsInterceptor);

                // registered editors
                foreach (T interceptor in interceptors)
                    yield return new KeyValuePair<IModMetadata, T>(metadata, interceptor);
            }
        }

        /// <summary>Dispose all game resources.</summary>
        /// <param name="disposing">Whether the content manager is disposing (rather than finalising).</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            // Clear cache & reload all assets. While that may seem perverse during disposal, it's
            // necessary due to limitations in the way SMAPI currently intercepts content assets.
            // 
            // The game uses multiple content managers while SMAPI needs one and only one. The game
            // only disposes some of its content managers when returning to title, which means SMAPI
            // can't know which assets are meant to be disposed. Here we remove current assets from
            // the cache, but don't dispose them to avoid crashing any code that still references
            // them. The garbage collector will eventually clean up any unused assets.
            this.Reset();
        }
    }
}
