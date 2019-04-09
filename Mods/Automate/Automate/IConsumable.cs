using StardewValley;

namespace Pathoschild.Stardew.Automate
{
    /// <summary>An ingredient stack (or stacks) which can be consumed by a machine.</summary>
    public interface IConsumable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The items available to consumable.</summary>
        ITrackedStack Consumables { get; }

        /// <summary>A sample item for comparison.</summary>
        /// <remarks>This should not be a reference to the original stack.</remarks>
        Item Sample { get; }

        /// <summary>The number of items needed for the recipe.</summary>
        int CountNeeded { get; }

        /// <summary>Whether the consumables needed for this requirement are ready.</summary>
        bool IsMet { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Remove the needed number of this item from the stack.</summary>
        void Reduce();

        /// <summary>Remove the needed number of this item from the stack and return a new stack matching the count.</summary>
        Item Take();
    }
}
