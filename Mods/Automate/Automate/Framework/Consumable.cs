using StardewValley;

namespace Pathoschild.Stardew.Automate.Framework
{
    /// <summary>An ingredient stack (or stacks) which can be consumed by a machine.</summary>
    internal class Consumable : IConsumable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The items available to consumable.</summary>
        public ITrackedStack Consumables { get; }

        /// <summary>A sample item for comparison.</summary>
        /// <remarks>This should not be a reference to the original stack.</remarks>
        public Item Sample => this.Consumables.Sample;

        /// <summary>The number of items needed for the recipe.</summary>
        public int CountNeeded { get; }

        /// <summary>Whether the consumables needed for this requirement are ready.</summary>
        public bool IsMet { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="consumables">The matching items available to consume.</param>
        /// <param name="countNeeded">The number of items needed for the recipe.</param>
        public Consumable(ITrackedStack consumables, int countNeeded)
        {
            this.Consumables = consumables;
            this.CountNeeded = countNeeded;
            this.IsMet = consumables.Count >= countNeeded;
        }

        /// <summary>Remove the needed number of this item from the stack.</summary>
        public void Reduce()
        {
            this.Consumables.Reduce(this.CountNeeded);
        }

        /// <summary>Remove the needed number of this item from the stack and return a new stack matching the count.</summary>
        public Item Take()
        {
            return this.Consumables.Take(this.CountNeeded);
        }
    }
}
