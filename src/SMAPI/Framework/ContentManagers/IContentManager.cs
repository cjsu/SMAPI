using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Framework.Exceptions;
using StardewValley;

namespace StardewModdingAPI.Framework.ContentManagers
{
    /// <summary>A content manager which handles reading files.</summary>
    internal interface IContentManager : IDisposable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A name for the mod manager. Not guaranteed to be unique.</summary>
        string Name { get; }

        /// <summary>The current language as a constant.</summary>
        LocalizedContentManager.LanguageCode Language { get; }

        /// <summary>The absolute path to the <see cref="ContentManager.RootDirectory"/>.</summary>
        string FullRootDirectory { get; }

        /// <summary>Whether this content manager can be targeted by managed asset keys (e.g. to load assets from a mod folder).</summary>
        bool IsNamespaced { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Load an asset that has been processed by the content pipeline.</summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="language">The language code for which to load content.</param>
        /// <param name="useCache">Whether to read/write the loaded asset to the asset cache.</param>
        T Load<T>(string assetName, LocalizedContentManager.LanguageCode language, bool useCache);

        /// <summary>Perform any cleanup needed when the locale changes.</summary>
        void OnLocaleChanged();

        /// <summary>Normalize path separators in a file path. For asset keys, see <see cref="AssertAndNormalizeAssetName"/> instead.</summary>
        /// <param name="path">The file path to normalize.</param>
        [Pure]
        string NormalizePathSeparators(string path);

        /// <summary>Assert that the given key has a valid format and return a normalized form consistent with the underlying cache.</summary>
        /// <param name="assetName">The asset key to check.</param>
        /// <exception cref="SContentLoadException">The asset key is empty or contains invalid characters.</exception>
        string AssertAndNormalizeAssetName(string assetName);

        /// <summary>Get the current content locale.</summary>
        string GetLocale();

        /// <summary>The locale for a language.</summary>
        /// <param name="language">The language.</param>
        string GetLocale(LocalizedContentManager.LanguageCode language);

        /// <summary>Get whether the content manager has already loaded and cached the given asset.</summary>
        /// <param name="assetName">The asset path relative to the loader root directory, not including the <c>.xnb</c> extension.</param>
        /// <param name="language">The language.</param>
        bool IsLoaded(string assetName, LocalizedContentManager.LanguageCode language);

        /// <summary>Get the cached asset keys.</summary>
        IEnumerable<string> GetAssetKeys();

        /// <summary>Purge matched assets from the cache.</summary>
        /// <param name="predicate">Matches the asset keys to invalidate.</param>
        /// <param name="dispose">Whether to dispose invalidated assets. This should only be <c>true</c> when they're being invalidated as part of a dispose, to avoid crashing the game.</param>
        /// <returns>Returns the invalidated asset names and instances.</returns>
        IDictionary<string, object> InvalidateCache(Func<string, Type, bool> predicate, bool dispose = false);
    }
}
