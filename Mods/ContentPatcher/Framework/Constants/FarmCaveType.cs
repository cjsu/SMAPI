using StardewValley;

namespace ContentPatcher.Framework.Constants
{
    /// <summary>A farm cave type.</summary>
    internal enum FarmCaveType
    {
        /// <summary>The player hasn't chosen a farm cave yet.</summary>
        None = Farmer.caveNothing,

        /// <summary>The fruit bat cave.</summary>
        Bats = Farmer.caveBats,

        /// <summary>The mushroom cave.</summary>
        Mushrooms = Farmer.caveMushrooms
    }
}
