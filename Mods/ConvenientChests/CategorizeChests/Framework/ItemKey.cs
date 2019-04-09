using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace ConvenientChests.CategorizeChests.Framework {
    internal struct ItemKey {
        public ItemType ItemType    { get; }
        public int      ObjectIndex { get; }

        public ItemKey(ItemType itemType, int parentSheetIndex) {
            ItemType    = itemType;
            ObjectIndex = parentSheetIndex;
        }

        public override int GetHashCode() => (int) ItemType * 10000 + ObjectIndex;

        public override string ToString() => $"{ItemType}:{ObjectIndex}";

        public override bool Equals(object obj) => obj is ItemKey itemKey          &&
                                                   itemKey.ItemType    == ItemType &&
                                                   itemKey.ObjectIndex == ObjectIndex;

        public Item GetOne() {
            switch (ItemType) {
                case ItemType.Boots:
                    return new Boots(ObjectIndex);

                case ItemType.Furniture:
                    return new Furniture(ObjectIndex, Vector2.Zero);

                case ItemType.Hat:
                    return new Hat(ObjectIndex);

                case ItemType.Fish:
                case ItemType.Object:
                case ItemType.BigCraftable:
                    return new Object(ObjectIndex, 1);

                case ItemType.Ring:
                    return new Ring(ObjectIndex);

                case ItemType.Tool:
                    return ToolFactory.getToolFromDescription((byte) ObjectIndex, Tool.stone);

                case ItemType.Wallpaper:
                    return new Wallpaper(ObjectIndex);

                case ItemType.Flooring:
                    return new Wallpaper(ObjectIndex, true);

                case ItemType.Weapon:
                    return new MeleeWeapon(ObjectIndex);

                case ItemType.Gate:
                    return new Fence(Vector2.Zero, ObjectIndex, true);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string GetCategory() {
            // move scythe to tools
            if (ItemType == ItemType.Weapon && ObjectIndex == MeleeWeapon.scythe)
                return Game1.content.LoadString("Strings\\StringsFromCSFiles:Tool.cs.14307");

            if (ItemType != ItemType.Object)
                return ItemType.ToString();

            var categoryName = GetOne().getCategoryName();
            return string.IsNullOrEmpty(categoryName) ? "Miscellaneous" : categoryName;
        }
    }
}