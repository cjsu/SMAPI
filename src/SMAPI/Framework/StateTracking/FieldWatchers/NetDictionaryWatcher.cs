using System.Collections.Generic;
using Netcode;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers
{
    /// <summary>A watcher which detects changes to a net dictionary field.</summary>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <typeparam name="TValue">The dictionary value type.</typeparam>
    /// <typeparam name="TField">The net type equivalent to <typeparamref name="TValue"/>.</typeparam>
    /// <typeparam name="TSerialDict">The serializable dictionary type that can store the keys and values.</typeparam>
    /// <typeparam name="TSelf">The net field instance type.</typeparam>
    internal class NetDictionaryWatcher<TKey, TValue, TField, TSerialDict, TSelf> : BaseDisposableWatcher, IDictionaryWatcher<TKey, TValue>
        where TField : class, INetObject<INetSerializable>, new()
        where TSerialDict : IDictionary<TKey, TValue>, new()
        where TSelf : NetDictionary<TKey, TValue, TField, TSerialDict, TSelf>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The pairs added since the last reset.</summary>
        private readonly IDictionary<TKey, TValue> PairsAdded = new Dictionary<TKey, TValue>();

        /// <summary>The pairs removed since the last reset.</summary>
        private readonly IDictionary<TKey, TValue> PairsRemoved = new Dictionary<TKey, TValue>();

        /// <summary>The field being watched.</summary>
        private readonly NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> Field;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the collection changed since the last reset.</summary>
        public bool IsChanged => this.PairsAdded.Count > 0 || this.PairsRemoved.Count > 0;

        /// <summary>The values added since the last reset.</summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> Added => this.PairsAdded;

        /// <summary>The values removed since the last reset.</summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> Removed => this.PairsRemoved;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="field">The field to watch.</param>
        public NetDictionaryWatcher(NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> field)
        {
            this.Field = field;

            field.OnValueAdded += this.OnValueAdded;
            field.OnValueRemoved += this.OnValueRemoved;
        }

        /// <summary>Update the current value if needed.</summary>
        public void Update()
        {
            this.AssertNotDisposed();
        }

        /// <summary>Set the current value as the baseline.</summary>
        public void Reset()
        {
            this.AssertNotDisposed();

            this.PairsAdded.Clear();
            this.PairsRemoved.Clear();
        }

        /// <summary>Stop watching the field and release all references.</summary>
        public override void Dispose()
        {
            if (!this.IsDisposed)
            {
                this.Field.OnValueAdded -= this.OnValueAdded;
                this.Field.OnValueRemoved -= this.OnValueRemoved;
            }
            base.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>A callback invoked when an entry is added to the dictionary.</summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">The entry value.</param>
        private void OnValueAdded(TKey key, TValue value)
        {
            this.PairsAdded[key] = value;
        }

        /// <summary>A callback invoked when an entry is removed from the dictionary.</summary>
        /// <param name="key">The entry key.</param>
        /// <param name="value">The entry value.</param>
        private void OnValueRemoved(TKey key, TValue value)
        {
            if (!this.PairsRemoved.ContainsKey(key))
                this.PairsRemoved[key] = value;
        }
    }
}
