using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Framework.Patching;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="SaveGame"/> which prevents some errors due to broken save data.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class LoadErrorPatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /// <summary>A callback invoked when custom content is removed from the save data to avoid a crash.</summary>
        private static Action OnContentRemoved;


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string Name => nameof(LoadErrorPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="onContentRemoved">A callback invoked when custom content is removed from the save data to avoid a crash.</param>
        public LoadErrorPatch(IMonitor monitor, Action onContentRemoved)
        {
            LoadErrorPatch.Monitor = monitor;
            LoadErrorPatch.OnContentRemoved = onContentRemoved;
        }


        /// <inheritdoc />
#if HARMONY_2
        public void Apply(Harmony harmony)
#else
        public void Apply(HarmonyInstance harmony)
#endif
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(SaveGame), nameof(SaveGame.loadDataToLocations)),
                prefix: new HarmonyMethod(this.GetType(), nameof(LoadErrorPatch.Before_SaveGame_LoadDataToLocations))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="SaveGame.loadDataToLocations"/>.</summary>
        /// <param name="gamelocations">The game locations being loaded.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_SaveGame_LoadDataToLocations(List<GameLocation> gamelocations)
        {
            bool removedAny =
                LoadErrorPatch.RemoveBrokenBuildings(gamelocations)
                | LoadErrorPatch.RemoveInvalidNpcs(gamelocations);

            if (removedAny)
                LoadErrorPatch.OnContentRemoved();

            return true;
        }

        /// <summary>Remove buildings which don't exist in the game data.</summary>
        /// <param name="locations">The current game locations.</param>
        private static bool RemoveBrokenBuildings(IEnumerable<GameLocation> locations)
        {
            bool removedAny = false;

            foreach (BuildableGameLocation location in locations.OfType<BuildableGameLocation>())
            {
                foreach (Building building in location.buildings.ToArray())
                {
                    try
                    {
                        BluePrint _ = new BluePrint(building.buildingType.Value);
                    }
                    catch (SContentLoadException)
                    {
                        LoadErrorPatch.Monitor.Log($"Removed invalid building type '{building.buildingType.Value}' in {location.Name} ({building.tileX}, {building.tileY}) to avoid a crash when loading save '{Constants.SaveFolderName}'. (Did you remove a custom building mod?)", LogLevel.Warn);
                        location.buildings.Remove(building);
                        removedAny = true;
                    }
                }
            }

            return removedAny;
        }

        /// <summary>Remove NPCs which don't exist in the game data.</summary>
        /// <param name="locations">The current game locations.</param>
        private static bool RemoveInvalidNpcs(IEnumerable<GameLocation> locations)
        {
            bool removedAny = false;

            IDictionary<string, string> data = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");
            foreach (GameLocation location in LoadErrorPatch.GetAllLocations(locations))
            {
                foreach (NPC npc in location.characters.ToArray())
                {
                    if (npc.isVillager() && !data.ContainsKey(npc.Name))
                    {
                        try
                        {
                            npc.reloadSprite(); // this won't crash for special villagers like Bouncer
                        }
                        catch
                        {
                            LoadErrorPatch.Monitor.Log($"Removed invalid villager '{npc.Name}' in {location.Name} ({npc.getTileLocation()}) to avoid a crash when loading save '{Constants.SaveFolderName}'. (Did you remove a custom NPC mod?)", LogLevel.Warn);
                            location.characters.Remove(npc);
                            removedAny = true;
                        }
                    }
                }
            }

            return removedAny;
        }

        /// <summary>Get all locations, including building interiors.</summary>
        /// <param name="locations">The main game locations.</param>
        private static IEnumerable<GameLocation> GetAllLocations(IEnumerable<GameLocation> locations)
        {
            foreach (GameLocation location in locations)
            {
                yield return location;
                if (location is BuildableGameLocation buildableLocation)
                {
                    foreach (GameLocation interior in buildableLocation.buildings.Select(p => p.indoors.Value).Where(p => p != null))
                        yield return interior;
                }
            }
        }
    }
}
