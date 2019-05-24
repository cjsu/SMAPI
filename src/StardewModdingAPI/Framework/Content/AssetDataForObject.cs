using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace StardewModdingAPI.Framework.Content
{
    /// <summary>Encapsulates access and changes to content being read from a data file.</summary>
    internal class AssetDataForObject : AssetData<object>, IAssetData
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locale">The content's locale code, if the content is localised.</param>
        /// <param name="assetName">The normalised asset name being read.</param>
        /// <param name="data">The content data being read.</param>
        /// <param name="getNormalisedPath">Normalises an asset key to match the cache key.</param>
        public AssetDataForObject(string locale, string assetName, object data, Func<string, string> getNormalisedPath)
            : base(locale, assetName, data, getNormalisedPath, onDataReplaced: null) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="info">The asset metadata.</param>
        /// <param name="data">The content data being read.</param>
        /// <param name="getNormalisedPath">Normalises an asset key to match the cache key.</param>
        public AssetDataForObject(IAssetInfo info, object data, Func<string, string> getNormalisedPath)
            : this(info.Locale, info.AssetName, data, getNormalisedPath) { }

        /// <summary>Get a helper to manipulate the data as a dictionary.</summary>
        /// <typeparam name="TKey">The expected dictionary key.</typeparam>
        /// <typeparam name="TValue">The expected dictionary balue.</typeparam>
        /// <exception cref="InvalidOperationException">The content being read isn't a dictionary.</exception>
        public IAssetDataForDictionary<TKey, TValue> AsDictionary<TKey, TValue>()
        {
            return new AssetDataForDictionary<TKey, TValue>(this.Locale, this.AssetName, this.GetData<IDictionary<TKey, TValue>>(), this.GetNormalisedPath, this.ReplaceWith);
        }

        /// <summary>Get a helper to manipulate the data as an image.</summary>
        /// <exception cref="InvalidOperationException">The content being read isn't an image.</exception>
        public IAssetDataForImage AsImage()
        {
            return new AssetDataForImage(this.Locale, this.AssetName, this.GetData<Texture2D>(), this.GetNormalisedPath, this.ReplaceWith);
        }

        /// <summary>Get the data as a given type.</summary>
        /// <typeparam name="TData">The expected data type.</typeparam>
        /// <exception cref="InvalidCastException">The data can't be converted to <typeparamref name="TData"/>.</exception>
        public TData GetData<TData>()
        {
            if (!(this.Data is TData))
                throw new InvalidCastException($"The content data of type {this.Data.GetType().FullName} can't be converted to the requested {typeof(TData).FullName}.");
            return (TData)this.Data;
        }
    }
}
