using System;
using System.Collections.Generic;

namespace Pathoschild.Stardew.Common.Utilities
{
    /// <summary>An implementation of <see cref="Dictionary{TKey,TValue}"/> whose keys are guaranteed to use <see cref="StringComparer.InvariantCultureIgnoreCase"/>.</summary>
    internal class InvariantDictionary<TValue> : Dictionary<string, TValue>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public InvariantDictionary()
            : base(StringComparer.InvariantCultureIgnoreCase) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="values">The values to add.</param>
        public InvariantDictionary(IDictionary<string, TValue> values)
            : base(values, StringComparer.InvariantCultureIgnoreCase) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="values">The values to add.</param>
        public InvariantDictionary(IEnumerable<KeyValuePair<string, TValue>> values)
            : base(StringComparer.InvariantCultureIgnoreCase)
        {
            foreach (var entry in values)
                this.Add(entry.Key, entry.Value);
        }
    }
}
