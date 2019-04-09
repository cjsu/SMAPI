using System.Collections.Generic;

namespace ConvenientChests.CategorizeChests.Framework
{
    /// <summary>
    /// Maintains the list of items that should be excluded from the available
    /// items to use for categorization, e.g. unobtainable items and bug items.
    /// </summary>
    static class ItemBlacklist
    {
        /// <summary>
        /// Check whether a given item key is blacklisted.
        /// </summary>
        /// <returns>Whether the key is blacklisted.</returns>
        /// <param name="itemKey">Item key to check.</param>
        public static bool Includes(ItemKey itemKey) => 
            itemKey.ItemType == ItemType.BigCraftable ||itemKey.ItemType == ItemType.Furniture || BlacklistedItemKeys.Contains(itemKey);

        private static readonly HashSet<ItemKey> BlacklistedItemKeys = new HashSet<ItemKey> {
            // stones
            new ItemKey(ItemType.Object, 2),
            new ItemKey(ItemType.Object, 4),
            new ItemKey(ItemType.Object, 75),
            new ItemKey(ItemType.Object, 76),
            new ItemKey(ItemType.Object, 77),
            new ItemKey(ItemType.Object, 290),
            new ItemKey(ItemType.Object, 343),
            new ItemKey(ItemType.Object, 450),
            new ItemKey(ItemType.Object, 668),
            new ItemKey(ItemType.Object, 670),
            new ItemKey(ItemType.Object, 751),
            new ItemKey(ItemType.Object, 760),
            new ItemKey(ItemType.Object, 762),
            new ItemKey(ItemType.Object, 764),
            new ItemKey(ItemType.Object, 765),

            // weeds
            new ItemKey(ItemType.Object, 0),
            new ItemKey(ItemType.Object, 313),
            new ItemKey(ItemType.Object, 314),
            new ItemKey(ItemType.Object, 315),
            new ItemKey(ItemType.Object, 316),
            new ItemKey(ItemType.Object, 317),
            new ItemKey(ItemType.Object, 318),
            new ItemKey(ItemType.Object, 319),
            new ItemKey(ItemType.Object, 320),
            new ItemKey(ItemType.Object, 321),
            new ItemKey(ItemType.Object, 452),
            new ItemKey(ItemType.Object, 674),
            new ItemKey(ItemType.Object, 675),
            new ItemKey(ItemType.Object, 676),
            new ItemKey(ItemType.Object, 677),
            new ItemKey(ItemType.Object, 678),
            new ItemKey(ItemType.Object, 679),
            new ItemKey(ItemType.Object, 750),
            new ItemKey(ItemType.Object, 784),
            new ItemKey(ItemType.Object, 785),
            new ItemKey(ItemType.Object, 786),
            new ItemKey(ItemType.Object, 792),
            new ItemKey(ItemType.Object, 793),
            new ItemKey(ItemType.Object, 794),

            // twigs
            new ItemKey(ItemType.Object, 294),
            new ItemKey(ItemType.Object, 295),

            new ItemKey(ItemType.Object, 30), // Lumber
            new ItemKey(ItemType.Object, 94), // Spirit Torch
            new ItemKey(ItemType.Object, 102), // Lost Book
            new ItemKey(ItemType.Object, 449), // Stone Base
            new ItemKey(ItemType.Object, 461), // Decorative Pot
            new ItemKey(ItemType.Object, 590), // Artifact Spot
            new ItemKey(ItemType.Object, 788), // Lost Axe
            new ItemKey(ItemType.Object, 789), // Lucky Purple Shorts
            new ItemKey(ItemType.Object, 790), // Berry Basket
            
            new ItemKey(ItemType.Weapon, 25), // Alex's Bat
            new ItemKey(ItemType.Weapon, 30), // Sam's Old Guitar
            new ItemKey(ItemType.Weapon, 35), // Elliott's Pencil
            new ItemKey(ItemType.Weapon, 36), // Maru's Wrench
            new ItemKey(ItemType.Weapon, 37), // Harvey's Mallet
            new ItemKey(ItemType.Weapon, 38), // Penny's Fryer
            new ItemKey(ItemType.Weapon, 39), // Leah's Whittler
            new ItemKey(ItemType.Weapon, 40), // Abby's Planchette
            new ItemKey(ItemType.Weapon, 41), // Seb's Lost Mace
            new ItemKey(ItemType.Weapon, 42), // Haley's Iron
            new ItemKey(ItemType.Weapon, 20), // Elf Blade
            new ItemKey(ItemType.Weapon, 34), // Galaxy Slingshot
            new ItemKey(ItemType.Weapon, 46), // Kudgel
            new ItemKey(ItemType.Weapon, 49), // Rapier
            new ItemKey(ItemType.Weapon, 19), // Shadow Dagger
            new ItemKey(ItemType.Weapon, 48), // Yeti Tooth

            new ItemKey(ItemType.Boots, 515), // Cowboy Boots
        };
    }
}
