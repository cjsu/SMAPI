using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers
{
    /// <summary>A watcher which detects changes to a value using a specified <see cref="IEqualityComparer{T}"/> instance.</summary>
    /// <typeparam name="TValue">The comparable value type.</typeparam>
    internal class ComparableWatcher<TValue> : IValueWatcher<TValue>
    {
        /*********
        ** Fields
        *********/
        /// <summary>Get the current value.</summary>
        private readonly Func<TValue> GetValue;

        /// <summary>The equality comparer.</summary>
        private readonly IEqualityComparer<TValue> Comparer;


        /*********
        ** Accessors
        *********/
        /// <summary>The field value at the last reset.</summary>
        public TValue PreviousValue { get; private set; }

        /// <summary>The latest value.</summary>
        public TValue CurrentValue { get; private set; }

        /// <summary>Whether the value changed since the last reset.</summary>
        public bool IsChanged { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="getValue">Get the current value.</param>
        /// <param name="comparer">The equality comparer which indicates whether two values are the same.</param>
        public ComparableWatcher(Func<TValue> getValue, IEqualityComparer<TValue> comparer)
        {
            this.GetValue = getValue;
            this.Comparer = comparer;
            this.PreviousValue = getValue();
        }

        /// <summary>Update the current value if needed.</summary>
        public void Update()
        {
            this.CurrentValue = this.GetValue();
            this.IsChanged = !this.Comparer.Equals(this.PreviousValue, this.CurrentValue);
        }

        /// <summary>Set the current value as the baseline.</summary>
        public void Reset()
        {
            this.PreviousValue = this.CurrentValue;
            this.IsChanged = false;
        }

        /// <summary>Release any references if needed when the field is no longer needed.</summary>
        public void Dispose() { }
    }
}
