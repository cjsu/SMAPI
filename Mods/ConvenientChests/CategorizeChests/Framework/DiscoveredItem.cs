using StardewValley;

namespace ConvenientChests.CategorizeChests.Framework
{
    class DiscoveredItem
    {
        public readonly ItemKey ItemKey;
        public readonly Item Item;

        public DiscoveredItem(ItemType type, int index, Item item)
        {
            ItemKey = new ItemKey(type, index);
            Item = item;
        }
    }
}