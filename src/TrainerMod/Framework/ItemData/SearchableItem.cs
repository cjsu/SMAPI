using StardewValley;

namespace TrainerMod.Framework.ItemData
{
    /// <summary>A game item with metadata.</summary>
    internal class SearchableItem
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The item type.</summary>
        public ItemType Type { get; }

        /// <summary>The item instance.</summary>
        public Item Item { get; }

        /// <summary>The item's unique ID for its type.</summary>
        public int ID { get; }

        /// <summary>The item's default name.</summary>
        public string Name => this.Item.Name;

        /// <summary>The item's display name for the current language.</summary>
        public string DisplayName => this.Item.DisplayName;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The item type.</param>
        /// <param name="id">The unique ID (if different from the item's parent sheet index).</param>
        /// <param name="item">The item instance.</param>
        public SearchableItem(ItemType type, int id, Item item)
        {
            this.Type = type;
            this.ID = id;
            this.Item = item;
        }
    }
}
