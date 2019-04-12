namespace Pathoschild.Stardew.LookupAnything.Framework
{
    /// <summary>The type of an in-game object for the mod's purposes.</summary>
    internal enum TargetType
    {
        /// <summary>The target type isn't recognised by the mod.</summary>
        Unknown,

        /****
        ** NPCs
        ****/
        /// <summary>A farm animal.</summary>
        FarmAnimal,

        /// <summary>A player's horse.</summary>
        Horse,

        /// <summary>A forest spirit.</summary>
        Junimo,

        /// <summary>A hostile monster NPC.</summary>
        Monster,

        /// <summary>A player's cat or dog.</summary>
        Pet,

        /// <summary>A player character.</summary>
        Farmer,

        /// <summary>A passive character NPC (including the dwarf and Krobus).</summary>
        Villager,

        /****
        ** Objects
        ****/
        /// <summary>An inventory item.</summary>
        InventoryItem,

        /// <summary>A map object.</summary>
        Object,

        /****
        ** Terrain features
        ****/
        /// <summary>A fruit tree.</summary>
        FruitTree,

        /// <summary>A non-fruit tree.</summary>
        WildTree,

        /// <summary>A terrain feature consisting of a tilled plot of land with a planted crop.</summary>
        Crop,

        /// <summary>A generic terrain feature.</summary>
        TerrainFeature,

        /****
        ** Other
        ****/
        /// <summary>A constructed building.</summary>
        Building,

        /// <summary>A map tile.</summary>
        Tile
    }
}
