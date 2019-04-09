using System.Linq;
using StardewModdingAPI;

namespace ConvenientChests.CategorizeChests.Framework.Persistence {
    /// <summary>
    /// The class responsible for saving and loading the mod state.
    /// </summary>
    class SaveManager : ISaveManager {
        private readonly CategorizeChestsModule Module;

        public SaveManager(CategorizeChestsModule module) {
            Module  = module;
        }

        /// <summary>
        /// Generate save data and write it to the given file path.
        /// </summary>
        /// <param name="relativePath">The path of the save file relative to the mod folder.</param>
        public void Save(string relativePath) {
            var saver = new Saver(Module.ChestDataManager);
            Module.ModEntry.Helper.Data.WriteJsonFile(relativePath, saver.GetSerializableData());
        }

        /// <summary>
        /// Load save data from the given file path.
        /// </summary>
        /// <param name="relativePath">The path of the save file relative to the mod folder.</param>
        public void Load(string relativePath) {
            var model = Module.ModEntry.Helper.Data.ReadJsonFile<SaveData>(relativePath) ?? new SaveData();

            foreach (var entry in model.ChestEntries) {
                try {
                    var chest     = Module.ChestFinder.GetChestByAddress(entry.Address);
                    var chestData = Module.ChestDataManager.GetChestData(chest);

                    chestData.AcceptedItemKinds = entry.GetItemSet();
                    foreach (var key in chestData.AcceptedItemKinds.Where(k => !Module.ItemDataManager.Prototypes.ContainsKey(k)))
                        Module.ItemDataManager.Prototypes.Add(key, key.GetOne());
                }
                catch (InvalidSaveDataException e) {
                    Module.Monitor.Log(e.Message, LogLevel.Warn);
                }
            }
        }
    }
}