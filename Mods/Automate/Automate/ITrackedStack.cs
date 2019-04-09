using StardewValley;

namespace Pathoschild.Stardew.Automate
{
    /// <summary>An item stack in an input pipe which can be reduced or taken.</summary>
    public interface ITrackedStack
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A sample item for comparison.</summary>
        /// <remarks>This should be equivalent to the underlying item (except in stack size), but *not* a reference to it.</remarks>
        Item Sample { get; }

        /// <summary>The number of items in the stack.</summary>
        int Count { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Remove the specified number of this item from the stack.</summary>
        /// <param name="count">The number to consume.</param>
        void Reduce(int count);

        /// <summary>Remove the specified number of this item from the stack and return a new stack matching the count.</summary>
        /// <param name="count">The number to get.</param>
        Item Take(int count);
    }
}
