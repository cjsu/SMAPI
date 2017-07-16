using System;
using System.Collections.Generic;

namespace StardewModdingAPI
{
    /// <summary>Encapsulates access and changes to dictionary content being read from a data file.</summary>
    public interface IAssetDataForDictionary<TKey, TValue> : IAssetData<IDictionary<TKey, TValue>>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Add or replace an entry in the dictionary.</summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">The entry value.</param>
        void Set(TKey key, TValue value);

        /// <summary>Add or replace an entry in the dictionary.</summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">A callback which accepts the current value and returns the new value.</param>
        void Set(TKey key, Func<TValue, TValue> value);

        /// <summary>Dynamically replace values in the dictionary.</summary>
        /// <param name="replacer">A lambda which takes the current key and value for an entry, and returns the new value.</param>
        void Set(Func<TKey, TValue, TValue> replacer);
    }
}
