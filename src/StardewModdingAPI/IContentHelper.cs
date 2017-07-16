using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace StardewModdingAPI
{
    /// <summary>Provides an API for loading content assets.</summary>
    public interface IContentHelper : IModLinked
    {
        /// <summary>Load content from the game folder or mod folder (if not already cached), and return it. When loading a <c>.png</c> file, this must be called outside the game's draw loop.</summary>
        /// <typeparam name="T">The expected data type. The main supported types are <see cref="Texture2D"/> and dictionaries; other types may be supported by the game's content pipeline.</typeparam>
        /// <param name="key">The asset key to fetch (if the <paramref name="source"/> is <see cref="ContentSource.GameContent"/>), or the local path to a content file relative to the mod folder.</param>
        /// <param name="source">Where to search for a matching content asset.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        /// <exception cref="ContentLoadException">The content asset couldn't be loaded (e.g. because it doesn't exist).</exception>
        T Load<T>(string key, ContentSource source = ContentSource.ModFolder);

        /// <summary>Get the underlying key in the game's content cache for an asset. This can be used to load custom map tilesheets, but should be avoided when you can use the content API instead. This does not validate whether the asset exists.</summary>
        /// <param name="key">The asset key to fetch (if the <paramref name="source"/> is <see cref="ContentSource.GameContent"/>), or the local path to a content file relative to the mod folder.</param>
        /// <param name="source">Where to search for a matching content asset.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        string GetActualAssetKey(string key, ContentSource source = ContentSource.ModFolder);
    }
}
