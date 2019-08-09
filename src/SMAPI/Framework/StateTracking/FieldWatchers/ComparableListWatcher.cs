using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers
{
    /// <summary>A watcher which detects changes to a collection of values using a specified <see cref="IEqualityComparer{T}"/> instance.</summary>
    /// <typeparam name="TValue">The value type within the collection.</typeparam>
    internal class ComparableListWatcher<TValue> : BaseDisposableWatcher, ICollectionWatcher<TValue>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The collection to watch.</summary>
        private readonly ICollection<TValue> CurrentValues;

        /// <summary>The values during the previous update.</summary>
        private HashSet<TValue> LastValues;

        /// <summary>The pairs added since the last reset.</summary>
        private readonly List<TValue> AddedImpl = new List<TValue>();

        /// <summary>The pairs removed since the last reset.</summary>
        private readonly List<TValue> RemovedImpl = new List<TValue>();


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the value changed since the last reset.</summary>
        public bool IsChanged => this.AddedImpl.Count > 0 || this.RemovedImpl.Count > 0;

        /// <summary>The values added since the last reset.</summary>
        public IEnumerable<TValue> Added => this.AddedImpl;

        /// <summary>The values removed since the last reset.</summary>
        public IEnumerable<TValue> Removed => this.RemovedImpl;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="values">The collection to watch.</param>
        /// <param name="comparer">The equality comparer which indicates whether two values are the same.</param>
        public ComparableListWatcher(ICollection<TValue> values, IEqualityComparer<TValue> comparer)
        {
            this.CurrentValues = values;
            this.LastValues = new HashSet<TValue>(comparer);
        }

        /// <summary>Update the current value if needed.</summary>
        public void Update()
        {
            this.AssertNotDisposed();

            // optimize for zero items
            if (this.CurrentValues.Count == 0)
            {
                if (this.LastValues.Count > 0)
                {
                    this.AddedImpl.AddRange(this.LastValues);
                    this.LastValues.Clear();
                }
                return;
            }

            // detect changes
            HashSet<TValue> curValues = new HashSet<TValue>(this.CurrentValues, this.LastValues.Comparer);
            this.RemovedImpl.AddRange(from value in this.LastValues where !curValues.Contains(value) select value);
            this.AddedImpl.AddRange(from value in curValues where !this.LastValues.Contains(value) select value);
            this.LastValues = curValues;
        }

        /// <summary>Set the current value as the baseline.</summary>
        public void Reset()
        {
            this.AssertNotDisposed();

            this.AddedImpl.Clear();
            this.RemovedImpl.Clear();
        }
    }
}
