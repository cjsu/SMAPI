using System.Collections.Generic;

namespace ConvenientChests.CategorizeChests.Framework.Persistence
{
    class SaveData
    {
        /// <summary>
        /// A list of chest addresses and the chest data associated with them.
        /// </summary>
        public IEnumerable<ChestEntry> ChestEntries;
    }
}