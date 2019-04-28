using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Represents an inventory slot that changed.</summary>
    public class ItemStackChange
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The item in the slot.</summary>
        public Item Item { get; set; }

        /// <summary>The amount by which the item's stack size changed.</summary>
        public int StackChange { get; set; }

        /// <summary>How the inventory slot changed.</summary>
        public ChangeType ChangeType { get; set; }
    }
}