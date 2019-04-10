using System.Collections.Generic;

namespace Pathoschild.Stardew.Common.Utilities
{
    /// <summary>A logical collection of values defined by restriction and exclusion values which may be infinite.</summary>
    /// <remarks>
    ///    <para>
    ///       Unlike a typical collection, a constraint set doesn't necessarily track the values it contains. For
    ///       example, a constraint set of <see cref="uint"/> values with one exclusion only stores one number but
    ///       logically contains <see cref="uint.MaxValue"/> elements.
    ///    </para>
    /// 
    ///    <para>
    ///       A constraint set is defined by two inner sets: <see cref="ExcludeValues"/> contains values which are
    ///       explicitly not part of the set, and <see cref="RestrictToValues"/> contains values which are explicitly
    ///       part of the set. Crucially, an empty <see cref="RestrictToValues"/> means an unbounded set (i.e. it
    ///       contains all possible values). If a value is part of both <see cref="ExcludeValues"/> and
    ///       <see cref="RestrictToValues"/>, the exclusion takes priority.
    ///    </para>
    /// </remarks>
    internal class ConstraintSet<T>
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The specific values to contain (or empty to match any value).</summary>
        public HashSet<T> RestrictToValues { get; }

        /// <summary>The specific values to exclude.</summary>
        public HashSet<T> ExcludeValues { get; }

        /// <summary>Whether the constraint set matches a finite set of values.</summary>
        public bool IsBounded => this.RestrictToValues.Count != 0;

        /// <summary>Get whether the constraint set logically matches an infinite set of values.</summary>
        public bool IsInfinite => !this.IsBounded;

        /// <summary>Whether there are any constraints placed on the set of values.</summary>
        public bool IsConstrained => this.RestrictToValues.Count != 0 || this.ExcludeValues.Count != 0;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ConstraintSet()
            : this(EqualityComparer<T>.Default) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="comparer">The equality comparer to use when comparing values in the set, or <see langword="null" /> to use the default implementation for the set type.</param>
        public ConstraintSet(IEqualityComparer<T> comparer)
        {
            this.RestrictToValues = new HashSet<T>(comparer);
            this.ExcludeValues = new HashSet<T>(comparer);
        }

        /// <summary>Bound the constraint set by adding the given value to the set of allowed values. If the constraint set is unbounded, this makes it bounded.</summary>
        /// <param name="value">The value.</param>
        /// <returns>Returns <c>true</c> if the value was added; else <c>false</c> if it was already present.</returns>
        public bool AddBound(T value)
        {
            return this.RestrictToValues.Add(value);
        }

        /// <summary>Bound the constraint set by adding the given values to the set of allowed values. If the constraint set is unbounded, this makes it bounded.</summary>
        /// <param name="values">The values.</param>
        /// <returns>Returns <c>true</c> if any value was added; else <c>false</c> if all values were already present.</returns>
        public bool AddBound(IEnumerable<T> values)
        {
            bool anyAdded = false;
            foreach (T value in values)
            {
                if (this.RestrictToValues.Add(value))
                    anyAdded = true;
            }
            return anyAdded;
        }

        /// <summary>Add values to exclude.</summary>
        /// <param name="value">The value to exclude.</param>
        /// <returns>Returns <c>true</c> if the value was added; else <c>false</c> if it was already present.</returns>
        public bool Exclude(T value)
        {
            return this.ExcludeValues.Add(value);
        }

        /// <summary>Add values to exclude.</summary>
        /// <param name="values">The values to exclude.</param>
        /// <returns>Returns <c>true</c> if any value was added; else <c>false</c> if all values were already present.</returns>
        public bool Exclude(IEnumerable<T> values)
        {
            bool anyAdded = false;
            foreach (T value in values)
            {
                if (this.ExcludeValues.Add(value))
                    anyAdded = true;
            }
            return anyAdded;
        }

        /// <summary>Get whether this constraint allows some values that would be allowed by another.</summary>
        /// <param name="other">The other </param>
        public bool Intersects(ConstraintSet<T> other)
        {
            // If both sets are unbounded, they're guaranteed to intersect since exclude can't be unbounded.
            if (this.IsInfinite && other.IsInfinite)
                return true;

            // if either set is bounded, they can only intersect in the included subset.
            if (this.IsBounded)
            {
                foreach (T value in this.RestrictToValues)
                {
                    if (this.Allows(value) && other.Allows(value))
                        return true;
                }
            }
            if (other.IsBounded)
            {
                foreach (T value in other.RestrictToValues)
                {
                    if (other.Allows(value) && this.Allows(value))
                        return true;
                }
            }

            // else no intersection
            return false;
        }

        /// <summary>Get whether the constraints allow the given value.</summary>
        /// <param name="value">The value to match.</param>
        public bool Allows(T value)
        {
            if (this.ExcludeValues.Contains(value))
                return false;

            return this.IsInfinite || this.RestrictToValues.Contains(value);
        }
    }
}
