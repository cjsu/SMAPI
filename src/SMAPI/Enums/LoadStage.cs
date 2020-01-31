namespace StardewModdingAPI.Enums
{
    /// <summary>A low-level stage in the game's loading process.</summary>
    public enum LoadStage
    {
        /// <summary>A save is not loaded or loading.</summary>
        None,

        /// <summary>The game is creating a new save slot, and has initialized the basic save info.</summary>
        CreatedBasicInfo,

        /// <summary>The game is creating a new save slot, and has initialized the in-game locations.</summary>
        CreatedLocations,

        /// <summary>The game is creating a new save slot, and has created the physical save files.</summary>
        CreatedSaveFile,

        /// <summary>The game is loading a save slot, and has read the raw save data into <see cref="StardewValley.SaveGame.loaded"/>. Not applicable when connecting to a multiplayer host. This is equivalent to <see cref="StardewValley.SaveGame.getLoadEnumerator"/> value 20.</summary>
        SaveParsed,

        /// <summary>The game is loading a save slot, and has applied the basic save info (including player data). Not applicable when connecting to a multiplayer host. Note that some basic info (like daily luck) is not initialized at this point. This is equivalent to <see cref="StardewValley.SaveGame.getLoadEnumerator"/> value 36.</summary>
        SaveLoadedBasicInfo,

        /// <summary>The game is loading a save slot, and has applied the in-game location data. Not applicable when connecting to a multiplayer host. This is equivalent to <see cref="StardewValley.SaveGame.getLoadEnumerator"/> value 50.</summary>
        SaveLoadedLocations,

        /// <summary>The final metadata has been loaded from the save file. This happens before the game applies problem fixes, checks for achievements, starts music, etc. Not applicable when connecting to a multiplayer host.</summary>
        Preloaded,

        /// <summary>The save is fully loaded, but the world may not be fully initialized yet.</summary>
        Loaded,

        /// <summary>The save is fully loaded, the world has been initialized, and <see cref="Context.IsWorldReady"/> is now true.</summary>
        Ready
    }
}
