using System.Collections.Generic;
using System.Linq;
using ConvenientChests.CategorizeChests.Framework;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;

namespace ConvenientChests.CategorizeChests {
    internal static class ItemHelper {
        public static ItemKey ToItemKey(this Item item) => new ItemKey(GetItemType(item), GetItemID(item));

        public static Item GetCopy(this Item item) {
            if (item == null)
                return null;

            var copy = item.getOne();
            copy.Stack = item.Stack;
            return copy;
        }

        public static IEnumerable<Item> GetWeapons() {
            foreach (var e in Game1.content.Load<Dictionary<int, string>>("Data\\weapons"))
                if (e.Value.Split('/')[8] == "4")
                    yield return new Slingshot(e.Key);
                
                else
                    yield return new MeleeWeapon(e.Key);
        }

        public static ItemType GetItemType(Item item) {
            switch (item) {
                case Boots _:
                    return ItemType.Boots;

                case Furniture _:
                    return ItemType.Furniture;

                case Hat _:
                    return ItemType.Hat;

                case Ring _:
                    return ItemType.Ring;

                case Wallpaper w:
                    return w.isFloor.Value
                               ? ItemType.Flooring
                               : ItemType.Wallpaper;

                case MeleeWeapon _:
                case Slingshot _:
                    return ItemType.Weapon;

                case Tool _:
                    return ItemType.Tool;

                case Fence f:
                    return f.isGate.Value
                               ? ItemType.Gate
                               : ItemType.Object;

                case Object _:
                    switch (item.Category) {
                        case Object.FishCategory:
                            return ItemType.Fish;

                        case Object.BigCraftableCategory:
                            return ItemType.BigCraftable;

                        default:
                            return ItemType.Object;
                    }
            }

            return ItemType.Object;
        }

        public static int GetItemID(Item item) {
            switch (item) {
                case Boots a:
                    return a.indexInTileSheet.Value;

                case Ring a:
                    return a.indexInTileSheet.Value;

                case Hat a:
                    return a.which.Value;

                case Tool a:
                    return a.InitialParentTileIndex;

                case Fence a:
                    if (a.isGate.Value)
                        return 0;

                    return a.whichType.Value;

                default:
                    return item.ParentSheetIndex;
            }
        }
    }
}