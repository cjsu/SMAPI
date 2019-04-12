namespace Pathoschild.Stardew.LookupAnything.Framework.Models
{
    /// <summary>Indicates an in-game recipe type.</summary>
    internal enum RecipeType
    {
        /// <summary>The recipe is cooked in the kitchen.</summary>
        Cooking,

        /// <summary>The recipe is crafted through the game menu.</summary>
        Crafting,

        /// <summary>The recipe represents the input for a crafting machine like a furnace.</summary>
        MachineInput,

        /// <summary>The recipe represents the materials needed to construct a building through Robin or the Wizard.</summary>
        BuildingBlueprint
    }
}
