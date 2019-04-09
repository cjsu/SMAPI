using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace Pathoschild.Stardew.Automate
{
    /// <summary>An item stack which wraps an underlying collection of stacks.</summary>
    public class TrackedItemCollection : ITrackedStack
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying item stacks.</summary>
        private readonly ITrackedStack[] Stacks;


        /*********
        ** Accessors
        *********/
        /// <summary>A sample item for comparison.</summary>
        /// <remarks>This should be equivalent to the underlying item (except in stack size), but *not* a reference to it.</remarks>
        public Item Sample { get; }

        /// <summary>The number of items in the stack.</summary>
        public int Count => this.Stacks.Sum(p => p.Count);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="stacks">The underlying item stacks.</param>
        public TrackedItemCollection(IEnumerable<ITrackedStack> stacks)
        {
            this.Stacks = stacks.ToArray();
            this.Sample = this.Stacks.FirstOrDefault()?.Sample;
        }

        /// <summary>Remove the specified number of this item from the stack.</summary>
        /// <param name="count">The number to consume.</param>
        public void Reduce(int count)
        {
            if (count <= 0 || !this.Stacks.Any())
                return;

            // reduce
            int left = count;
            foreach (ITrackedStack stack in this.Stacks)
            {
                // skip, stack empty
                if (stack.Count <= 0)
                    continue;

                // take entire stack
                if (stack.Count < left)
                {
                    left -= stack.Count;
                    stack.Reduce(stack.Count);
                    continue;
                }

                // take remaining items
                stack.Reduce(left);
                break;
            }
        }

        /// <summary>Remove the specified number of this item from the stack and return a new stack matching the count.</summary>
        /// <param name="count">The number to get.</param>
        public Item Take(int count)
        {
            if (count <= 0 || !this.Stacks.Any())
                return null;

            // reduce
            this.Reduce(count);

            // create new stack
            Item item = this.Sample.getOne();
            item.Stack = count;
            return item;
        }
    }
}
