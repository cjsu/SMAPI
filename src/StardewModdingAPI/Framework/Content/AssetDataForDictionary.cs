using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Framework.Content
{
    /// <summary>Encapsulates access and changes to dictionary content being read from a data file.</summary>
    internal class AssetDataForDictionary<TKey, TValue> : AssetData<IDictionary<TKey, TValue>>, IAssetDataForDictionary<TKey, TValue>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locale">The content's locale code, if the content is localised.</param>
        /// <param name="assetName">The normalised asset name being read.</param>
        /// <param name="data">The content data being read.</param>
        /// <param name="getNormalisedPath">Normalises an asset key to match the cache key.</param>
        public AssetDataForDictionary(string locale, string assetName, IDictionary<TKey, TValue> data, Func<string, string> getNormalisedPath)
            : base(locale, assetName, data, getNormalisedPath) { }

        /// <summary>Add or replace an entry in the dictionary.</summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">The entry value.</param>
        public void Set(TKey key, TValue value)
        {
            this.Data[key] = value;
        }

        /// <summary>Add or replace an entry in the dictionary.</summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">A callback which accepts the current value and returns the new value.</param>
        public void Set(TKey key, Func<TValue, TValue> value)
        {
            this.Data[key] = value(this.Data[key]);
        }

        /// <summary>Dynamically replace values in the dictionary.</summary>
        /// <param name="replacer">A lambda which takes the current key and value for an entry, and returns the new value.</param>
        public void Set(Func<TKey, TValue, TValue> replacer)
        {
            foreach (var pair in this.Data.ToArray())
                this.Data[pair.Key] = replacer(pair.Key, pair.Value);
        }
    }
}
