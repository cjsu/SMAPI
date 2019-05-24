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
        /// <param name="onDataReplaced">A callback to invoke when the data is replaced (if any).</param>
        public AssetDataForDictionary(string locale, string assetName, IDictionary<TKey, TValue> data, Func<string, string> getNormalisedPath, Action<IDictionary<TKey, TValue>> onDataReplaced)
            : base(locale, assetName, data, getNormalisedPath, onDataReplaced) { }

#if !SMAPI_3_0_STRICT
        /// <summary>Add or replace an entry in the dictionary.</summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">The entry value.</param>
        [Obsolete("Access " + nameof(AssetData<IDictionary<TKey, TValue>>.Data) + "field directly.")]
        public void Set(TKey key, TValue value)
        {
            SCore.DeprecationManager.Warn($"AssetDataForDictionary.{nameof(Set)}", "2.10", DeprecationLevel.PendingRemoval);
            this.Data[key] = value;
        }

        /// <summary>Add or replace an entry in the dictionary.</summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">A callback which accepts the current value and returns the new value.</param>
        [Obsolete("Access " + nameof(AssetData<IDictionary<TKey, TValue>>.Data) + "field directly.")]
        public void Set(TKey key, Func<TValue, TValue> value)
        {
            SCore.DeprecationManager.Warn($"AssetDataForDictionary.{nameof(Set)}", "2.10", DeprecationLevel.PendingRemoval);
            this.Data[key] = value(this.Data[key]);
        }

        /// <summary>Dynamically replace values in the dictionary.</summary>
        /// <param name="replacer">A lambda which takes the current key and value for an entry, and returns the new value.</param>
        [Obsolete("Access " + nameof(AssetData<IDictionary<TKey, TValue>>.Data) + "field directly.")]
        public void Set(Func<TKey, TValue, TValue> replacer)
        {
            SCore.DeprecationManager.Warn($"AssetDataForDictionary.{nameof(Set)}", "2.10", DeprecationLevel.PendingRemoval);
            foreach (var pair in this.Data.ToArray())
                this.Data[pair.Key] = replacer(pair.Key, pair.Value);
        }
#endif
    }
}
