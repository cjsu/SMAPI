#if !SMAPI_3_0_STRICT
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="PlayerEvents.InventoryChanged"/> event.</summary>
    public class EventArgsInventoryChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The player's inventory.</summary>
        public IList<Item> Inventory { get; }

        /// <summary>The added items.</summary>
        public List<ItemStackChange> Added { get; }

        /// <summary>The removed items.</summary>
        public List<ItemStackChange> Removed { get; }

        /// <summary>The items whose stack sizes changed.</summary>
        public List<ItemStackChange> QuantityChanged { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="inventory">The player's inventory.</param>
        /// <param name="changedItems">The inventory changes.</param>
        public EventArgsInventoryChanged(IList<Item> inventory, ItemStackChange[] changedItems)
        {
            this.Inventory = inventory;
            this.Added = changedItems.Where(n => n.ChangeType == ChangeType.Added).ToList();
            this.Removed = changedItems.Where(n => n.ChangeType == ChangeType.Removed).ToList();
            this.QuantityChanged = changedItems.Where(n => n.ChangeType == ChangeType.StackChange).ToList();
        }
    }
}
#endif
