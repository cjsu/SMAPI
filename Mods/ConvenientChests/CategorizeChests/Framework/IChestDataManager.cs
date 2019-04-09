using StardewValley.Objects;

namespace ConvenientChests.CategorizeChests.Framework
{
    /// <summary>
    /// An interface for retrieving the mod-specific data associated with a
    /// given Stardew Valley chest object.
    /// </summary>
    internal interface IChestDataManager
    {
        ChestData GetChestData(Chest chest);
    }
}