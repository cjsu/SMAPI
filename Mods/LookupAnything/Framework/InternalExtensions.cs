using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using StardewValley;
using StardewValley.Objects;
using Object = StardewValley.Object;

namespace Pathoschild.Stardew.LookupAnything.Framework
{
    /// <summary>Provides utility extension methods.</summary>
    internal static class InternalExtensions
    {
        /*********
        ** Public methods
        *********/
        /****
        ** Items
        ****/
        /// <summary>Get the sprite sheet to which the item's <see cref="Item.parentSheetIndex"/> refers.</summary>
        /// <param name="item">The item to check.</param>
        public static ItemSpriteType GetSpriteType(this Item item)
        {
            if (item is Object obj)
            {
                if (obj is Furniture)
                    return ItemSpriteType.Furniture;
                if (obj is Wallpaper)
                    return ItemSpriteType.Wallpaper;
                return obj.bigCraftable.Value
                    ? ItemSpriteType.BigCraftable
                    : ItemSpriteType.Object;
            }
            if (item is Boots)
                return ItemSpriteType.Boots;
            if (item is Hat)
                return ItemSpriteType.Hat;
            if (item is Tool)
                return ItemSpriteType.Tool;

            return ItemSpriteType.Unknown;
        }
    }
}
