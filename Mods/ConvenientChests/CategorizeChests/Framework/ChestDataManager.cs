using System.Runtime.CompilerServices;
using StardewValley.Objects;

namespace ConvenientChests.CategorizeChests.Framework
{
    class ChestDataManager : IChestDataManager
    {
        private readonly ConditionalWeakTable<Chest, ChestData> _table = new ConditionalWeakTable<Chest, ChestData>();

        public ChestData GetChestData(Chest chest) => _table.GetValue(chest, c => new ChestData(c));
    }
}