namespace Pathoschild.Stardew.LookupAnything.Framework.Constants
{
    /// <summary>Indicates the sprite sheet used to draw an object. A given sprite ID can be duplicated between two sprite sheets.</summary>
    internal enum ItemSpriteType
    {
        /// <summary>The <c>Data\ObjectInformation.xnb</c> (<see cref="StardewValley.Game1.objectSpriteSheet"/>) sprite sheet used to draw most inventory items and some placeable objects.</summary>
        Object,

        /// <summary>The <c>Data\BigCraftablesInformation.xnb</c> (<see cref="StardewValley.Game1.bigCraftableSpriteSheet"/>) sprite sheet used to draw furniture, scarecrows, tappers, crafting stations, and similar placeable objects.</summary>
        BigCraftable,

        /// <summary>The <c>Data\Boots.xnb</c> sprite sheet used to draw boot equipment.</summary>
        Boots,

        /// <summary>The <c>Data\hats.xnb</c> sprite sheet used to draw boot equipment.</summary>
        Hat,

        /// <summary>The <c>TileSheets\furniture.xnb</c> sprite sheet used to draw furniture.</summary>
        Furniture,

        /// <summary>The <c>TileSheets\weapons.xnb</c> sprite sheet used to draw tools and weapons.</summary>
        Tool,

        /// <summary>The <c>Maps\walls_and_floors</c> sprite sheet used to draw wallpapers and flooring.</summary>
        Wallpaper,

        /// <summary>The item isn't covered by one of the known types.</summary>
        Unknown
    }
}
