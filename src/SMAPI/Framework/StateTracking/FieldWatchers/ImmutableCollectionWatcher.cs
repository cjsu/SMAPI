using System.Collections.Generic;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers
{
    /// <summary>A collection watcher which never changes.</summary>
    /// <typeparam name="TValue">The value type within the collection.</typeparam>
    internal class ImmutableCollectionWatcher<TValue> : BaseDisposableWatcher, ICollectionWatcher<TValue>
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A singleton collection watcher instance.</summary>
        public static ImmutableCollectionWatcher<TValue> Instance { get; } = new ImmutableCollectionWatcher<TValue>();

        /// <summary>Whether the collection changed since the last reset.</summary>
        public bool IsChanged { get; } = false;

        /// <summary>The values added since the last reset.</summary>
        public IEnumerable<TValue> Added { get; } = new TValue[0];

        /// <summary>The values removed since the last reset.</summary>
        public IEnumerable<TValue> Removed { get; } = new TValue[0];


        /*********
        ** Public methods
        *********/
        /// <summary>Update the current value if needed.</summary>
        public void Update() { }

        /// <summary>Set the current value as the baseline.</summary>
        public void Reset() { }

        /// <summary>Stop watching the field and release all references.</summary>
        public override void Dispose() { }
    }
}
