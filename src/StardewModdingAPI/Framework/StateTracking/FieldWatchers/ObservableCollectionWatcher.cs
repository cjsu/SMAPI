using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers
{
    /// <summary>A watcher which detects changes to an observable collection.</summary>
    /// <typeparam name="TValue">The value type within the collection.</typeparam>
    internal class ObservableCollectionWatcher<TValue> : BaseDisposableWatcher, ICollectionWatcher<TValue>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The field being watched.</summary>
        private readonly ObservableCollection<TValue> Field;

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
        public ObservableCollectionWatcher(ObservableCollection<TValue> field)
        {
            this.Field = field;
            field.CollectionChanged += this.OnCollectionChanged;
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
                this.Field.CollectionChanged -= this.OnCollectionChanged;
            base.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>A callback invoked when an entry is added or removed from the collection.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                this.AddedImpl.AddRange(e.NewItems.Cast<TValue>());
            if (e.OldItems != null)
                this.RemovedImpl.AddRange(e.OldItems.Cast<TValue>());
        }
    }
}
