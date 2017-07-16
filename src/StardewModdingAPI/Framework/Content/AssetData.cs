using System;

namespace StardewModdingAPI.Framework.Content
{
    /// <summary>Base implementation for a content helper which encapsulates access and changes to content being read from a data file.</summary>
    /// <typeparam name="TValue">The interface value type.</typeparam>
    internal class AssetData<TValue> : AssetInfo, IAssetData<TValue>
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The content data being read.</summary>
        public TValue Data { get; protected set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locale">The content's locale code, if the content is localised.</param>
        /// <param name="assetName">The normalised asset name being read.</param>
        /// <param name="data">The content data being read.</param>
        /// <param name="getNormalisedPath">Normalises an asset key to match the cache key.</param>
        public AssetData(string locale, string assetName, TValue data, Func<string, string> getNormalisedPath)
            : base(locale, assetName, data.GetType(), getNormalisedPath)
        {
            this.Data = data;
        }

        /// <summary>Replace the entire content value with the given value. This is generally not recommended, since it may break compatibility with other mods or different versions of the game.</summary>
        /// <param name="value">The new content value.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidCastException">The <paramref name="value"/>'s type is not compatible with the loaded asset's type.</exception>
        public void ReplaceWith(TValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Can't set a loaded asset to a null value.");
            if (!this.DataType.IsInstanceOfType(value))
                throw new InvalidCastException($"Can't replace loaded asset of type {this.GetFriendlyTypeName(this.DataType)} with value of type {this.GetFriendlyTypeName(value.GetType())}. The new type must be compatible to prevent game errors.");

            this.Data = value;
        }
    }
}
