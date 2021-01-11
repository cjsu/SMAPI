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

        /// <summary>The previous values as of the last update.</summary>
        private readonly List<TValue> PreviousValues = new List<TValue>();


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
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.RemovedImpl.AddRange(this.PreviousValues);
                this.PreviousValues.Clear();
            }
            else
            {
                TValue[] added = e.NewItems?.Cast<TValue>().ToArray();
                TValue[] removed = e.OldItems?.Cast<TValue>().ToArray();

                if (removed != null)
                {
                    this.RemovedImpl.AddRange(removed);
                    this.PreviousValues.RemoveRange(e.OldStartingIndex, removed.Length);
                }
                if (added != null)
                {
                    this.AddedImpl.AddRange(added);
                    this.PreviousValues.InsertRange(e.NewStartingIndex, added);
                }
            }
        }
    }
}
