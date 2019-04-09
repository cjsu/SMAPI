using StardewValley.Objects;

namespace ConvenientChests.CategorizeChests.Framework
{
    /// <summary>
    /// A tool for moving items in bulk from the player's inventory
    /// into a given chest according to that chest's settings.
    /// </summary>
    public interface IChestFiller
    {
        void DumpItemsToChest(Chest chest);
    }
}