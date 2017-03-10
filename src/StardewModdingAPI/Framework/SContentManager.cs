using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using StardewModdingAPI.AssemblyRewriters;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>SMAPI's implementation of the game's content manager which lets it raise content events.</summary>
    internal class SContentManager : LocalizedContentManager
    {
        /*********
        ** Accessors
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
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public SContentManager(IServiceProvider serviceProvider, string rootDirectory, IMonitor monitor)
            : this(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, null, monitor) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localise content.</param>
        /// <param name="languageCodeOverride">The current language code for which to localise content.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public SContentManager(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, string languageCodeOverride, IMonitor monitor)
            : base(serviceProvider, rootDirectory, currentCulture, languageCodeOverride)
        {
            // initialise
            this.Monitor = monitor;
            IReflectionHelper reflection = new ReflectionHelper();

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

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        public override T Load<T>(string assetName)
        {
            // normalise asset name so can override the cache value later
            assetName = this.NormaliseAssetName(assetName);

            // skip if no event handlers or already loaded
            if (!ContentEvents.HasAssetLoadingListeners() || this.Cache.ContainsKey(assetName))
                return base.Load<T>(assetName);

            // intercept load
            T data = default(T);
            try {
                data = base.Load<T>(assetName);
            } catch (Exception) {
                // Looks like the asset doesn't exist in the game's Content directory!
            }
            string cacheLocale = this.GetCacheLocale(assetName, this.Cache);
            IContentEventHelper helper = new ContentEventHelper(cacheLocale, assetName, data, this.NormaliseAssetName);
            ContentEvents.InvokeAssetLoading(this.Monitor, helper);
            this.Cache[assetName] = helper.Data;
            return (T)helper.Data;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Normalise an asset name so it's consistent with the underlying cache.</summary>
        /// <param name="assetName">The asset key.</param>
        private string NormaliseAssetName(string assetName)
        {
            // ensure name format is consistent
            string[] parts = assetName.Split(SContentManager.PossiblePathSeparators, StringSplitOptions.RemoveEmptyEntries);
            assetName = string.Join(SContentManager.PreferredPathSeparator, parts);

            // apply platform normalisation logic
            return this.NormaliseAssetNameForPlatform(assetName);
        }

        /// <summary>Get the locale for which the asset name was saved, if any.</summary>
        /// <param name="normalisedAssetName">The normalised asset name.</param>
        /// <param name="cache">The cache to search.</param>
        private string GetCacheLocale(string normalisedAssetName, IDictionary<string, object> cache)
        {
            string locale = this.GetKeyLocale.Invoke<string>();
            return this.Cache.ContainsKey($"{normalisedAssetName}.{this.GetKeyLocale.Invoke<string>()}")
                ? locale
                : null;
        }
    }
}
