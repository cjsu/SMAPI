using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace ConvenientChests.CategorizeChests.Framework {
    /// <summary>
    /// A repository of item data that maps item keys to representative items
    /// and vice versa.
    /// </summary>
    internal interface IItemDataManager {
        Dictionary<string, IList<ItemKey>> Categories { get; }
        Dictionary<ItemKey, Item>          Prototypes { get; }

        Item    GetItem(ItemKey itemKey);
        ItemKey GetItemKey(Item item);
    }
}