using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files from a SMAPI mod folder with support for unpacked files.</summary>
    internal abstract class BaseContentManager : LocalizedContentManager, IContentManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>The central coordinator which manages content managers.</summary>
        protected readonly ContentCoordinator Coordinator;

        /// <summary>The underlying asset cache.</summary>
        protected readonly ContentCache Cache;

        /// <summary>Encapsulates monitoring and logging.</summary>
        protected readonly IMonitor Monitor;

        /// <summary>Whether the content coordinator has been disposed.</summary>
        private bool IsDisposed;

        /// <summary>A callback to invoke when the content manager is being disposed.</summary>
        private readonly Action<BaseContentManager> OnDisposing;

        /// <summary>The language enum values indexed by locale code.</summary>
        protected IDictionary<string, LanguageCode> LanguageCodes { get; }

        /// <summary>A list of disposable assets.</summary>
        private readonly List<WeakReference<IDisposable>> Disposables = new List<WeakReference<IDisposable>>();


        /*********
        ** Accessors
        *********/
        /// <summary>A name for the mod manager. Not guaranteed to be unique.</summary>
        public string Name { get; }

        /// <summary>The current language as a constant.</summary>
        public LanguageCode Language => this.GetCurrentLanguage();

        /// <summary>The absolute path to the <see cref="ContentManager.RootDirectory"/>.</summary>
        public string FullRootDirectory => Path.Combine(Constants.ExecutionPath, this.RootDirectory);

        /// <summary>Whether this content manager can be targeted by managed asset keys (e.g. to load assets from a mod folder).</summary>
        public bool IsNamespaced { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">A name for the mod manager. Not guaranteed to be unique.</param>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        /// <param name="currentCulture">The current culture for which to localize content.</param>
        /// <param name="coordinator">The central coordinator which manages content managers.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="onDisposing">A callback to invoke when the content manager is being disposed.</param>
        /// <param name="isNamespaced">Whether this content manager handles managed asset keys (e.g. to load assets from a mod folder).</param>
        protected BaseContentManager(string name, IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture, ContentCoordinator coordinator, IMonitor monitor, Reflector reflection, Action<BaseContentManager> onDisposing, bool isNamespaced)
                : base(serviceProvider, rootDirectory, currentCulture)
        {
            // init
            this.Name = name;
            this.Coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            this.Cache = new ContentCache(this, reflection);
            this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            this.OnDisposing = onDisposing;
            this.IsNamespaced = isNamespaced;

            // get asset data
            this.LanguageCodes = this.GetKeyLocales().ToDictionary(p => p.Value, p => p.Key, StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        public override T Load<T>(string assetName)
        {
            return this.Load<T>(assetName, this.Language, useCache: true);
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="language">The language code for which to load content.</param>
        public override T Load<T>(string assetName, LanguageCode language)
        {
            return this.Load<T>(assetName, language, useCache: true);
        }

        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="language">The language code for which to load content.</param>
        /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
        public abstract T Load<T>(string assetName, LocalizedContentManager.LanguageCode language, bool useCache);

        /// <summary>Load the base asset without localization.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        [Obsolete("This method is implemented for the base game and should not be used directly. To load an asset from the underlying content manager directly, use " + nameof(BaseContentManager.RawLoad) + " instead.")]
        public override T LoadBase<T>(string assetName)
        {
            return this.Load<T>(assetName, LanguageCode.en, useCache: true);
        }

        /// <summary>Perform any cleanup needed when the locale changes.</summary>
        public virtual void OnLocaleChanged() { }

        /// <summary>Normalize path separators in a file path. For asset keys, see <see cref="AssertAndNormalizeAssetName"/> instead.</summary>
        /// <param name="path">The file path to normalize.</param>
        [Pure]
        public string NormalizePathSeparators(string path)
        {
            return this.Cache.NormalizePathSeparators(path);
        }

        /// <summary>Assert that the given key has a valid format and return a normalized form consistent with the underlying cache.</summary>
        /// <param name="assetName">The asset key to check.</param>
        /// <exception cref="SContentLoadException">The asset key is empty or contains invalid characters.</exception>
        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local", Justification = "Parameter is only used for assertion checks by design.")]
        public string AssertAndNormalizeAssetName(string assetName)
        {
            // NOTE: the game checks for ContentLoadException to handle invalid keys, so avoid
            // throwing other types like ArgumentException here.
            if (string.IsNullOrWhiteSpace(assetName))
                throw new SContentLoadException("The asset key or local path is empty.");
            if (assetName.Intersect(Path.GetInvalidPathChars()).Any())
                throw new SContentLoadException("The asset key or local path contains invalid characters.");

            return this.Cache.NormalizeKey(assetName);
        }

        /****
        ** Content loading
        ****/
        /// <summary>Get the current content locale.</summary>
        public string GetLocale()
        {
            return this.GetLocale(this.GetCurrentLanguage());
        }

        /// <summary>The locale for a language.</summary>
        /// <param name="language">The language.</param>
        public string GetLocale(LanguageCode language)
        {
            return this.LanguageCodeString(language);
        }

        /// <summary>Get whether the content manager has already loaded and cached the given asset.</summary>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        public bool IsLoaded(string assetName)
        {
            assetName = this.Cache.NormalizeKey(assetName);
            return this.IsNormalizedKeyLoaded(assetName);
        }

        /// <summary>Get the cached asset keys.</summary>
        public IEnumerable<string> GetAssetKeys()
        {
            return this.Cache.Keys
                .Select(this.GetAssetName)
                .Distinct();
        }

        /****
        ** Cache invalidation
        ****/
        /// <summary>Purge matched assets from the cache.</summary>
        /// <param name="predicate">Matches the asset keys to invalidate.</param>
        /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
        /// <returns>Returns the invalidated asset names and types.</returns>
        public IEnumerable<Tuple<string, Type>> InvalidateCache(Func<string, Type, bool> predicate, bool dispose = false)
        {
            Dictionary<string, Type> removeAssetNames = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
            this.Cache.Remove((key, type) =>
            {
                this.ParseCacheKey(key, out string assetName, out _);

                if (removeAssetNames.ContainsKey(assetName))
                    return true;
                if (predicate(assetName, type))
                {
                    removeAssetNames[assetName] = type;
                    return true;
                }
                return false;
            });

            return removeAssetNames.Select(p => Tuple.Create(p.Key, p.Value));
        }

        /// <summary>Dispose held resources.</summary>
        /// <param name="isDisposing">Whether the content manager is being disposed (rather than finalized).</param>
        protected override void Dispose(bool isDisposing)
        {
            // ignore if disposed
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;

            // dispose uncached assets
            foreach (WeakReference<IDisposable> reference in this.Disposables)
            {
                if (reference.TryGetTarget(out IDisposable disposable))
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch { /* ignore dispose errors */ }
                }
            }
            this.Disposables.Clear();

            // raise event
            this.OnDisposing(this);

            base.Dispose(isDisposing);
        }

        /// <inheritdoc />
        public override void Unload()
        {
            if (this.IsDisposed)
                return; // base logic doesn't allow unloading twice, which happens due to SMAPI and the game both unloading

            base.Unload();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Load an asset file directly from the underlying content manager.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The normalized asset key.</param>
        /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
        protected virtual T RawLoad<T>(string assetName, bool useCache)
        {
            return useCache
                ? base.LoadBase<T>(assetName)
                : base.ReadAsset<T>(assetName, disposable => this.Disposables.Add(new WeakReference<IDisposable>(disposable)));
        }

        /// <summary>Inject an asset into the cache.</summary>
        /// <typeparam name="T">The type of asset to inject.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="value">The asset value.</param>
        /// <param name="language">The language code for which to inject the asset.</param>
        protected virtual void Inject<T>(string assetName, T value, LanguageCode language)
        {
            assetName = this.AssertAndNormalizeAssetName(assetName);
            this.Cache[assetName] = value;
        }

        /// <summary>Parse a cache key into its component parts.</summary>
        /// <param name="cacheKey">The input cache key.</param>
        /// <param name="assetName">The original asset name.</param>
        /// <param name="localeCode">The asset locale code (or <c>null</c> if not localized).</param>
        protected void ParseCacheKey(string cacheKey, out string assetName, out string localeCode)
        {
            // handle localized key
            if (!string.IsNullOrWhiteSpace(cacheKey))
            {
                int lastSepIndex = cacheKey.LastIndexOf(".", StringComparison.InvariantCulture);
                if (lastSepIndex >= 0)
                {
                    string suffix = cacheKey.Substring(lastSepIndex + 1, cacheKey.Length - lastSepIndex - 1);
                    if (this.LanguageCodes.ContainsKey(suffix))
                    {
                        assetName = cacheKey.Substring(0, lastSepIndex);
                        localeCode = cacheKey.Substring(lastSepIndex + 1, cacheKey.Length - lastSepIndex - 1);
                        return;
                    }
                }
            }

            // handle simple key
            assetName = cacheKey;
            localeCode = null;
        }

        /// <summary>Get whether an asset has already been loaded.</summary>
        /// <param name="normalizedAssetName">The normalized asset name.</param>
        protected abstract bool IsNormalizedKeyLoaded(string normalizedAssetName);

        /// <summary>Get the locale codes (like <c>ja-JP</c>) used in asset keys.</summary>
        private IDictionary<LanguageCode, string> GetKeyLocales()
        {
            // create locale => code map
            IDictionary<LanguageCode, string> map = new Dictionary<LanguageCode, string>();
            foreach (LanguageCode code in Enum.GetValues(typeof(LanguageCode)))
                map[code] = this.GetLocale(code);

            return map;
        }

        /// <summary>Get the asset name from a cache key.</summary>
        /// <param name="cacheKey">The input cache key.</param>
        private string GetAssetName(string cacheKey)
        {
            this.ParseCacheKey(cacheKey, out string assetName, out string _);
            return assetName;
        }
    }
}
