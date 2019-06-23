using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for an <see cref="IPlayerEvents.InventoryChanged"/> event.</summary>
    public class InventoryChangedEventArgs : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The player whose inventory changed.</summary>
        public Farmer Player { get; }

        /// <summary>The added items.</summary>
        public IEnumerable<Item> Added { get; }

        /// <summary>The removed items.</summary>
        public IEnumerable<Item> Removed { get; }

        /// <summary>The items whose stack sizes changed, with the relative change.</summary>
        public IEnumerable<ItemStackSizeChange> QuantityChanged { get; }

        /// <summary>Whether the affected player is the local one.</summary>
        public bool IsLocalPlayer => this.Player.IsLocalPlayer;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="player">The player whose inventory changed.</param>
        /// <param name="changedItems">The inventory changes.</param>
        internal InventoryChangedEventArgs(Farmer player, ItemStackChange[] changedItems)
        {
            this.Player = player;
            this.Added = changedItems
                .Where(n => n.ChangeType == ChangeType.Added)
                .Select(p => p.Item)
                .ToArray();

            this.Removed = changedItems
                .Where(n => n.ChangeType == ChangeType.Removed)
                .Select(p => p.Item)
                .ToArray();

            this.QuantityChanged = changedItems
                .Where(n => n.ChangeType == ChangeType.StackChange)
                .Select(change => new ItemStackSizeChange(
                    item: change.Item,
                    oldSize: change.Item.Stack - change.StackChange,
                    newSize: change.Item.Stack
                ))
                .ToArray();
        }
    }
}
