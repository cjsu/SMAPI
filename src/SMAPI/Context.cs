using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI
{
    /// <summary>Provides information about the current game state.</summary>
    public static class Context
    {
        /*********
        ** Accessors
        *********/
        /****
        ** Public
        ****/
        /// <summary>Whether the game has performed core initialisation. This becomes true right before the first update tick..</summary>
        public static bool IsGameLaunched { get; internal set; }

        /// <summary>Whether the player has loaded a save and the world has finished initialising.</summary>
        public static bool IsWorldReady { get; internal set; }

        /// <summary>Whether <see cref="IsWorldReady"/> is true and the player is free to act in the world (no menu is displayed, no cutscene is in progress, etc).</summary>
        public static bool IsPlayerFree => Context.IsWorldReady && Game1.currentLocation != null && Game1.activeClickableMenu == null && !Game1.dialogueUp && (!Game1.eventUp || Game1.isFestival());

        /// <summary>Whether <see cref="IsPlayerFree"/> is true and the player is free to move (e.g. not using a tool).</summary>
        public static bool CanPlayerMove => Context.IsPlayerFree && Game1.player.CanMove;

        /// <summary>Whether the game is currently running the draw loop. This isn't relevant to most mods, since you should use <see cref="IDisplayEvents"/> events to draw to the screen.</summary>
        public static bool IsInDrawLoop { get; internal set; }

        /// <summary>Whether <see cref="IsWorldReady"/> and the player loaded the save in multiplayer mode (regardless of whether any other players are connected).</summary>
        public static bool IsMultiplayer => Context.IsWorldReady && Game1.multiplayerMode != Game1.singlePlayer;

        /// <summary>Whether <see cref="IsWorldReady"/> and the current player is the main player. This is always true in single-player, and true when hosting in multiplayer.</summary>
        public static bool IsMainPlayer => Context.IsWorldReady && Game1.IsMasterGame;

        /****
        ** Internal
        ****/
        /// <summary>Whether a player save has been loaded.</summary>
        internal static bool IsSaveLoaded => Game1.hasLoadedGame && !(Game1.activeClickableMenu is TitleMenu);

        /// <summary>Whether the game is currently writing to the save file.</summary>
        internal static bool IsSaving => Game1.activeClickableMenu is SaveGameMenu || Game1.activeClickableMenu is ShippingMenu; // saving is performed by SaveGameMenu, but it's wrapped by ShippingMenu on days when the player shipping something

        /// <summary>The current stage in the game's loading process.</summary>
        internal static LoadStage LoadStage { get; set; }
    }
}
