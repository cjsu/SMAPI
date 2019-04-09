using System.Collections.Generic;
using System.Linq;

namespace ConvenientChests.CategorizeChests.Framework.Persistence
{
    /// <summary>
    /// A piece of saved data describing the location of a chest and what items
    /// the chest at that location has been assigned to.
    /// </summary>
    class ChestEntry
    {
        /// <summary>
        /// The chest's location in the world.
        /// </summary>
        public ChestAddress Address;

        /// <summary>
        /// The set of item keys that were configured to be accepted
        /// by the chest at <see cref="Address"/> .
        /// </summary>
        public Dictionary<ItemType, string> AcceptedItems;


        public ChestEntry()
        {
        }

        public ChestEntry(ChestData data, ChestAddress address)
        {
            Address = address;
            AcceptedItems = data.AcceptedItemKinds
                .GroupBy(i => i.ItemType)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(",", g.Select(i => i.ObjectIndex))
                );
        }


        public HashSet<ItemKey> GetItemSet() =>
            new HashSet<ItemKey>(AcceptedItems.SelectMany(e => e.Value.Split(',').Select(i => new ItemKey(e.Key, int.Parse(i)))));
    }
}