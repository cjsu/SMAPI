using System.Collections.Generic;
using Netcode;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers
{
    /// <summary>A watcher which detects changes to a Netcode collection.</summary>
    /// <typeparam name="TValue">The value type within the collection.</typeparam>
    internal class NetCollectionWatcher<TValue> : BaseDisposableWatcher, ICollectionWatcher<TValue>
        where TValue : class, INetObject<INetSerializable>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The field being watched.</summary>
        private readonly NetCollection<TValue> Field;

        /// <summary>The pairs added since the last reset.</summary>
        private readonly List<TValue> AddedImpl = new List<TValue>();

        /// <summary>The pairs removed since the last reset.</summary>
        private readonly List<TValue> RemovedImpl = new List<TValue>();


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the collection changed since the last reset.</summary>
        public bool IsChanged => this.AddedImpl.Count > 0 || this.RemovedImpl.Count > 0;

        /// <summary>The values added since the last reset.</summary>
        public IEnumerable<TValue> Added => this.AddedImpl;

        /// <summary>The values removed since the last reset.</summary>
        public IEnumerable<TValue> Removed => this.RemovedImpl;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="field">The field to watch.</param>
        public NetCollectionWatcher(NetCollection<TValue> field)
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

            this.AddedImpl.Clear();
            this.RemovedImpl.Clear();
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
        /// <summary>A callback invoked when an entry is added to the collection.</summary>
        /// <param name="value">The added value.</param>
        private void OnValueAdded(TValue value)
        {
            this.AddedImpl.Add(value);
        }

        /// <summary>A callback invoked when an entry is removed from the collection.</summary>
        /// <param name="value">The added value.</param>
        private void OnValueRemoved(TValue value)
        {
            this.RemovedImpl.Add(value);
        }
    }
}
