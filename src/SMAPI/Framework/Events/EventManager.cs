using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>Manages SMAPI events.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Private fields are deliberately named to simplify organisation.")]
    internal class EventManager
    {
        /*********
        ** Events (new)
        *********/
        /****
        ** Display
        ****/
        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        public readonly ManagedEvent<MenuChangedEventArgs> MenuChanged;

        /// <summary>Raised before the game draws anything to the screen in a draw tick, as soon as the sprite batch is opened. The sprite batch may be closed and reopened multiple times after this event is called, but it's only raised once per draw tick. This event isn't useful for drawing to the screen, since the game will draw over it.</summary>
        public readonly ManagedEvent<RenderingEventArgs> Rendering;

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen. Since the game may open/close the sprite batch multiple times in a draw tick, the sprite batch may not contain everything being drawn and some things may already be rendered to the screen. Content drawn to the sprite batch at this point will be drawn over all vanilla content (including menus, HUD, and cursor).</summary>
        public readonly ManagedEvent<RenderedEventArgs> Rendered;

        /// <summary>Raised before the game world is drawn to the screen.</summary>
        public readonly ManagedEvent<RenderingWorldEventArgs> RenderingWorld;

        /// <summary>Raised after the game world is drawn to the sprite patch, before it's rendered to the screen.</summary>
        public readonly ManagedEvent<RenderedWorldEventArgs> RenderedWorld;

        /// <summary>When a menu is open (<see cref="StardewValley.Game1.activeClickableMenu"/> isn't null), raised before that menu is drawn to the screen.</summary>
        public readonly ManagedEvent<RenderingActiveMenuEventArgs> RenderingActiveMenu;

        /// <summary>When a menu is open (<see cref="StardewValley.Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        public readonly ManagedEvent<RenderedActiveMenuEventArgs> RenderedActiveMenu;

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen.</summary>
        public readonly ManagedEvent<RenderingHudEventArgs> RenderingHud;

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        public readonly ManagedEvent<RenderedHudEventArgs> RenderedHud;

        /// <summary>Raised after the game window is resized.</summary>
        public readonly ManagedEvent<WindowResizedEventArgs> WindowResized;

        /****
        ** Game loop
        ****/
        /// <summary>Raised after the game is launched, right before the first update tick.</summary>
        public readonly ManagedEvent<GameLaunchedEventArgs> GameLaunched;

        /// <summary>Raised before the game performs its overall update tick (≈60 times per second).</summary>
        public readonly ManagedEvent<UpdateTickingEventArgs> UpdateTicking;

        /// <summary>Raised after the game performs its overall update tick (≈60 times per second).</summary>
        public readonly ManagedEvent<UpdateTickedEventArgs> UpdateTicked;

        /// <summary>Raised once per second before the game performs its overall update tick.</summary>
        public readonly ManagedEvent<OneSecondUpdateTickingEventArgs> OneSecondUpdateTicking;

        /// <summary>Raised once per second after the game performs its overall update tick.</summary>
        public readonly ManagedEvent<OneSecondUpdateTickedEventArgs> OneSecondUpdateTicked;

        /// <summary>Raised before the game creates the save file.</summary>
        public readonly ManagedEvent<SaveCreatingEventArgs> SaveCreating;

        /// <summary>Raised after the game finishes creating the save file.</summary>
        public readonly ManagedEvent<SaveCreatedEventArgs> SaveCreated;

        /// <summary>Raised before the game begins writes data to the save file (except the initial save creation).</summary>
        public readonly ManagedEvent<SavingEventArgs> Saving;

        /// <summary>Raised after the game finishes writing data to the save file (except the initial save creation).</summary>
        public readonly ManagedEvent<SavedEventArgs> Saved;

        /// <summary>Raised after the player loads a save slot and the world is initialized.</summary>
        public readonly ManagedEvent<SaveLoadedEventArgs> SaveLoaded;

        /// <summary>Raised after the game begins a new day, including when loading a save.</summary>
        public readonly ManagedEvent<DayStartedEventArgs> DayStarted;

        /// <summary>Raised before the game ends the current day. This happens before it starts setting up the next day and before <see cref="Saving"/>.</summary>
        public readonly ManagedEvent<DayEndingEventArgs> DayEnding;

        /// <summary>Raised after the in-game clock time changes.</summary>
        public readonly ManagedEvent<TimeChangedEventArgs> TimeChanged;

        /// <summary>Raised after the game returns to the title screen.</summary>
        public readonly ManagedEvent<ReturnedToTitleEventArgs> ReturnedToTitle;

        /****
        ** Input
        ****/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        public readonly ManagedEvent<ButtonPressedEventArgs> ButtonPressed;

        /// <summary>Raised after the player released a button on the keyboard, controller, or mouse.</summary>
        public readonly ManagedEvent<ButtonReleasedEventArgs> ButtonReleased;

        /// <summary>Raised after the player moves the in-game cursor.</summary>
        public readonly ManagedEvent<CursorMovedEventArgs> CursorMoved;

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        public readonly ManagedEvent<MouseWheelScrolledEventArgs> MouseWheelScrolled;

        /****
        ** Multiplayer
        ****/
        /// <summary>Raised after the mod context for a peer is received. This happens before the game approves the connection, so the player doesn't yet exist in the game. This is the earliest point where messages can be sent to the peer via SMAPI.</summary>
        public readonly ManagedEvent<PeerContextReceivedEventArgs> PeerContextReceived;

        /// <summary>Raised after a mod message is received over the network.</summary>
        public readonly ManagedEvent<ModMessageReceivedEventArgs> ModMessageReceived;

        /// <summary>Raised after the connection with a peer is severed.</summary>
        public readonly ManagedEvent<PeerDisconnectedEventArgs> PeerDisconnected;

        /****
        ** Player
        ****/
        /// <summary>Raised after items are added or removed to a player's inventory.</summary>
        public readonly ManagedEvent<InventoryChangedEventArgs> InventoryChanged;

        /// <summary>Raised after a player skill level changes. This happens as soon as they level up, not when the game notifies the player after their character goes to bed.</summary>
        public readonly ManagedEvent<LevelChangedEventArgs> LevelChanged;

        /// <summary>Raised after a player warps to a new location.</summary>
        public readonly ManagedEvent<WarpedEventArgs> Warped;

        /****
        ** World
        ****/
        /// <summary>Raised after a game location is added or removed.</summary>
        public readonly ManagedEvent<LocationListChangedEventArgs> LocationListChanged;

        /// <summary>Raised after buildings are added or removed in a location.</summary>
        public readonly ManagedEvent<BuildingListChangedEventArgs> BuildingListChanged;

        /// <summary>Raised after debris are added or removed in a location.</summary>
        public readonly ManagedEvent<DebrisListChangedEventArgs> DebrisListChanged;

        /// <summary>Raised after large terrain features (like bushes) are added or removed in a location.</summary>
        public readonly ManagedEvent<LargeTerrainFeatureListChangedEventArgs> LargeTerrainFeatureListChanged;

        /// <summary>Raised after NPCs are added or removed in a location.</summary>
        public readonly ManagedEvent<NpcListChangedEventArgs> NpcListChanged;

        /// <summary>Raised after objects are added or removed in a location.</summary>
        public readonly ManagedEvent<ObjectListChangedEventArgs> ObjectListChanged;

        /// <summary>Raised after terrain features (like floors and trees) are added or removed in a location.</summary>
        public readonly ManagedEvent<TerrainFeatureListChangedEventArgs> TerrainFeatureListChanged;

        /****
        ** Specialized
        ****/
        /// <summary>Raised when the low-level stage in the game's loading process has changed. See notes on <see cref="ISpecializedEvents.LoadStageChanged"/>.</summary>
        public readonly ManagedEvent<LoadStageChangedEventArgs> LoadStageChanged;

        /// <summary>Raised before the game performs its overall update tick (≈60 times per second). See notes on <see cref="ISpecializedEvents.UnvalidatedUpdateTicking"/>.</summary>
        public readonly ManagedEvent<UnvalidatedUpdateTickingEventArgs> UnvalidatedUpdateTicking;

        /// <summary>Raised after the game performs its overall update tick (≈60 times per second). See notes on <see cref="ISpecializedEvents.UnvalidatedUpdateTicked"/>.</summary>
        public readonly ManagedEvent<UnvalidatedUpdateTickedEventArgs> UnvalidatedUpdateTicked;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Writes messages to the log.</param>
        /// <param name="modRegistry">The mod registry with which to identify mods.</param>
        public EventManager(IMonitor monitor, ModRegistry modRegistry)
        {
            // create shortcut initializers
            ManagedEvent<TEventArgs> ManageEventOf<TEventArgs>(string typeName, string eventName) => new ManagedEvent<TEventArgs>($"{typeName}.{eventName}", monitor, modRegistry);

            // init events (new)
            this.MenuChanged = ManageEventOf<MenuChangedEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.MenuChanged));
            this.Rendering = ManageEventOf<RenderingEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.Rendering));
            this.Rendered = ManageEventOf<RenderedEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.Rendered));
            this.RenderingWorld = ManageEventOf<RenderingWorldEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderingWorld));
            this.RenderedWorld = ManageEventOf<RenderedWorldEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderedWorld));
            this.RenderingActiveMenu = ManageEventOf<RenderingActiveMenuEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderingActiveMenu));
            this.RenderedActiveMenu = ManageEventOf<RenderedActiveMenuEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderedActiveMenu));
            this.RenderingHud = ManageEventOf<RenderingHudEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderingHud));
            this.RenderedHud = ManageEventOf<RenderedHudEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.RenderedHud));
            this.WindowResized = ManageEventOf<WindowResizedEventArgs>(nameof(IModEvents.Display), nameof(IDisplayEvents.WindowResized));

            this.GameLaunched = ManageEventOf<GameLaunchedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.GameLaunched));
            this.UpdateTicking = ManageEventOf<UpdateTickingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.UpdateTicking));
            this.UpdateTicked = ManageEventOf<UpdateTickedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.UpdateTicked));
            this.OneSecondUpdateTicking = ManageEventOf<OneSecondUpdateTickingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.OneSecondUpdateTicking));
            this.OneSecondUpdateTicked = ManageEventOf<OneSecondUpdateTickedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.OneSecondUpdateTicked));
            this.SaveCreating = ManageEventOf<SaveCreatingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.SaveCreating));
            this.SaveCreated = ManageEventOf<SaveCreatedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.SaveCreated));
            this.Saving = ManageEventOf<SavingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.Saving));
            this.Saved = ManageEventOf<SavedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.Saved));
            this.SaveLoaded = ManageEventOf<SaveLoadedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.SaveLoaded));
            this.DayStarted = ManageEventOf<DayStartedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.DayStarted));
            this.DayEnding = ManageEventOf<DayEndingEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.DayEnding));
            this.TimeChanged = ManageEventOf<TimeChangedEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.TimeChanged));
            this.ReturnedToTitle = ManageEventOf<ReturnedToTitleEventArgs>(nameof(IModEvents.GameLoop), nameof(IGameLoopEvents.ReturnedToTitle));

            this.ButtonPressed = ManageEventOf<ButtonPressedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.ButtonPressed));
            this.ButtonReleased = ManageEventOf<ButtonReleasedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.ButtonReleased));
            this.CursorMoved = ManageEventOf<CursorMovedEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.CursorMoved));
            this.MouseWheelScrolled = ManageEventOf<MouseWheelScrolledEventArgs>(nameof(IModEvents.Input), nameof(IInputEvents.MouseWheelScrolled));

            this.PeerContextReceived = ManageEventOf<PeerContextReceivedEventArgs>(nameof(IModEvents.Multiplayer), nameof(IMultiplayerEvents.PeerContextReceived));
            this.ModMessageReceived = ManageEventOf<ModMessageReceivedEventArgs>(nameof(IModEvents.Multiplayer), nameof(IMultiplayerEvents.ModMessageReceived));
            this.PeerDisconnected = ManageEventOf<PeerDisconnectedEventArgs>(nameof(IModEvents.Multiplayer), nameof(IMultiplayerEvents.PeerDisconnected));

            this.InventoryChanged = ManageEventOf<InventoryChangedEventArgs>(nameof(IModEvents.Player), nameof(IPlayerEvents.InventoryChanged));
            this.LevelChanged = ManageEventOf<LevelChangedEventArgs>(nameof(IModEvents.Player), nameof(IPlayerEvents.LevelChanged));
            this.Warped = ManageEventOf<WarpedEventArgs>(nameof(IModEvents.Player), nameof(IPlayerEvents.Warped));

            this.BuildingListChanged = ManageEventOf<BuildingListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.LocationListChanged));
            this.DebrisListChanged = ManageEventOf<DebrisListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.DebrisListChanged));
            this.LargeTerrainFeatureListChanged = ManageEventOf<LargeTerrainFeatureListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.LargeTerrainFeatureListChanged));
            this.LocationListChanged = ManageEventOf<LocationListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.BuildingListChanged));
            this.NpcListChanged = ManageEventOf<NpcListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.NpcListChanged));
            this.ObjectListChanged = ManageEventOf<ObjectListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.ObjectListChanged));
            this.TerrainFeatureListChanged = ManageEventOf<TerrainFeatureListChangedEventArgs>(nameof(IModEvents.World), nameof(IWorldEvents.TerrainFeatureListChanged));

            this.LoadStageChanged = ManageEventOf<LoadStageChangedEventArgs>(nameof(IModEvents.Specialized), nameof(ISpecializedEvents.LoadStageChanged));
            this.UnvalidatedUpdateTicking = ManageEventOf<UnvalidatedUpdateTickingEventArgs>(nameof(IModEvents.Specialized), nameof(ISpecializedEvents.UnvalidatedUpdateTicking));
            this.UnvalidatedUpdateTicked = ManageEventOf<UnvalidatedUpdateTickedEventArgs>(nameof(IModEvents.Specialized), nameof(ISpecializedEvents.UnvalidatedUpdateTicked));
        }
    }
}
