using System;
using System.Collections.Generic;

namespace Pathoschild.Stardew.Common.Utilities
{
    /// <summary>An implementation of <see cref="HashSet{T}"/> for strings which always uses <see cref="StringComparer.InvariantCultureIgnoreCase"/>.</summary>
    internal class InvariantHashSet : HashSet<string>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public InvariantHashSet()
            : base(StringComparer.InvariantCultureIgnoreCase) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="values">The values to add.</param>
        public InvariantHashSet(IEnumerable<string> values)
            : base(values, StringComparer.InvariantCultureIgnoreCase) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="value">The single value to add.</param>
        public InvariantHashSet(string value)
            : base(new[] { value }, StringComparer.InvariantCultureIgnoreCase) { }

        /// <summary>Get a hashset for boolean true/false.</summary>
        public static InvariantHashSet Boolean()
        {
            return new InvariantHashSet(new[] { "true", "false" });
        }
    }
}
