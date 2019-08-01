using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.Networking;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.StateTracking.Snapshots;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;
using xTile.Dimensions;

namespace StardewModdingAPI.Framework
{
    /// <summary>SMAPI's extension of the game's core <see cref="Game1"/>, used to inject events.</summary>
    internal class SGame : Game1
    {
        /*********
        ** Fields
        *********/
        /****
        ** SMAPI state
        ****/
        /// <summary>Encapsulates monitoring and logging for SMAPI.</summary>
        private readonly Monitor Monitor;

        /// <summary>Encapsulates monitoring and logging on the game's behalf.</summary>
        private readonly IMonitor MonitorForGame;

        /// <summary>Manages SMAPI events for mods.</summary>
        private readonly EventManager Events;

        /// <summary>Tracks the installed mods.</summary>
        private readonly ModRegistry ModRegistry;

        /// <summary>Manages deprecation warnings.</summary>
        private readonly DeprecationManager DeprecationManager;

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from a draw error.</summary>
        private readonly Countdown DrawCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from an update error.</summary>
        private readonly Countdown UpdateCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>The number of ticks until SMAPI should notify mods that the game has loaded.</summary>
        /// <remarks>Skipping a few frames ensures the game finishes initialising the world before mods try to change it.</remarks>
        private readonly Countdown AfterLoadTimer = new Countdown(5);

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        private bool IsBetweenSaveEvents;

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        private bool IsBetweenCreateEvents;

        /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
        private readonly Action OnLoadingFirstAsset;

        /// <summary>A callback to invoke after the game finishes initialising.</summary>
        private readonly Action OnGameInitialised;

        /// <summary>A callback to invoke when the game exits.</summary>
        private readonly Action OnGameExiting;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Propagates notification that SMAPI should exit.</summary>
        private readonly CancellationTokenSource CancellationToken;

        /****
        ** Game state
        ****/
        /// <summary>Monitors the entire game state for changes.</summary>
        private WatcherCore Watchers;

        /// <summary>A snapshot of the current <see cref="Watchers"/> state.</summary>
        private WatcherSnapshot WatcherSnapshot = new WatcherSnapshot();

        /// <summary>Whether post-game-startup initialisation has been performed.</summary>
        private bool IsInitialised;

        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;


        /*********
        ** Accessors
        *********/
        /// <summary>Static state to use while <see cref="Game1"/> is initialising, which happens before the <see cref="SGame"/> constructor runs.</summary>
        internal static SGameConstructorHack ConstructorHack { get; set; }

        /// <summary>The number of update ticks which have already executed. This is similar to <see cref="Game1.ticks"/>, but incremented more consistently for every tick.</summary>
        internal static uint TicksElapsed { get; private set; }

        /// <summary>SMAPI's content manager.</summary>
        public ContentCoordinator ContentCore { get; private set; }

        /// <summary>Manages console commands.</summary>
        public CommandManager CommandManager { get; } = new CommandManager();

        /// <summary>Manages input visible to the game.</summary>
        public SInputState Input => (SInputState)Game1.input;

        /// <summary>The game's core multiplayer utility.</summary>
        public SMultiplayer Multiplayer => (SMultiplayer)Game1.multiplayer;

        /// <summary>A list of queued commands to execute.</summary>
        /// <remarks>This property must be threadsafe, since it's accessed from a separate console input thread.</remarks>
        public ConcurrentQueue<string> CommandQueue { get; } = new ConcurrentQueue<string>();


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
        /// <param name="monitorForGame">Encapsulates monitoring and logging on the game's behalf.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="eventManager">Manages SMAPI events for mods.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        /// <param name="modRegistry">Tracks the installed mods.</param>
        /// <param name="deprecationManager">Manages deprecation warnings.</param>
        /// <param name="onGameInitialised">A callback to invoke after the game finishes initialising.</param>
        /// <param name="onGameExiting">A callback to invoke when the game exits.</param>
        /// <param name="cancellationToken">Propagates notification that SMAPI should exit.</param>
        /// <param name="logNetworkTraffic">Whether to log network traffic.</param>
        internal SGame(Monitor monitor, IMonitor monitorForGame, Reflector reflection, EventManager eventManager, JsonHelper jsonHelper, ModRegistry modRegistry, DeprecationManager deprecationManager, Action onGameInitialised, Action onGameExiting, CancellationTokenSource cancellationToken, bool logNetworkTraffic)
        {
            this.OnLoadingFirstAsset = SGame.ConstructorHack.OnLoadingFirstAsset;
            SGame.ConstructorHack = null;

            // check expectations
            if (this.ContentCore == null)
                throw new InvalidOperationException($"The game didn't initialise its first content manager before SMAPI's {nameof(SGame)} constructor. This indicates an incompatible lifecycle change.");

            // init XNA
            Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // init SMAPI
            this.Monitor = monitor;
            this.MonitorForGame = monitorForGame;
            this.Events = eventManager;
            this.ModRegistry = modRegistry;
            this.Reflection = reflection;
            this.DeprecationManager = deprecationManager;
            this.OnGameInitialised = onGameInitialised;
            this.OnGameExiting = onGameExiting;
            Game1.input = new SInputState();
            Game1.multiplayer = new SMultiplayer(monitor, eventManager, jsonHelper, modRegistry, reflection, this.OnModMessageReceived, logNetworkTraffic);
            Game1.hooks = new SModHooks(this.OnNewDayAfterFade);
            this.CancellationToken = cancellationToken;

            // init observables
            Game1.locations = new ObservableCollection<GameLocation>();
        }

        /// <summary>Initialise just before the game's first update tick.</summary>
        private void InitialiseAfterGameStarted()
        {
            // set initial state
            this.Input.TrueUpdate();

            // init watchers
            this.Watchers = new WatcherCore(this.Input);

            // raise callback
            this.OnGameInitialised();
        }

        /// <summary>Perform cleanup logic when the game exits.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event args.</param>
        /// <remarks>This overrides the logic in <see cref="Game1.exitEvent"/> to let SMAPI clean up before exit.</remarks>
        protected override void OnExiting(object sender, EventArgs args)
        {
            Game1.multiplayer.Disconnect(StardewValley.Multiplayer.DisconnectType.ClosedGame);
            this.OnGameExiting?.Invoke();
        }

        /// <summary>A callback invoked before <see cref="Game1.newDayAfterFade"/> runs.</summary>
        protected void OnNewDayAfterFade()
        {
            this.Events.DayEnding.RaiseEmpty();
        }

        /// <summary>A callback invoked when a mod message is received.</summary>
        /// <param name="message">The message to deliver to applicable mods.</param>
        private void OnModMessageReceived(ModMessageModel message)
        {
            // raise events for applicable mods
            HashSet<string> modIDs = new HashSet<string>(message.ToModIDs ?? this.ModRegistry.GetAll().Select(p => p.Manifest.UniqueID), StringComparer.InvariantCultureIgnoreCase);
            this.Events.ModMessageReceived.RaiseForMods(new ModMessageReceivedEventArgs(message), mod => mod != null && modIDs.Contains(mod.Manifest.UniqueID));
        }

        /// <summary>A callback invoked when the game's low-level load stage changes.</summary>
        /// <param name="newStage">The new load stage.</param>
        internal void OnLoadStageChanged(LoadStage newStage)
        {
            // nothing to do
            if (newStage == Context.LoadStage)
                return;

            // update data
            LoadStage oldStage = Context.LoadStage;
            Context.LoadStage = newStage;
            if (newStage == LoadStage.None)
            {
                this.Monitor.Log("Context: returned to title", LogLevel.Trace);
                this.Multiplayer.CleanupOnMultiplayerExit();
            }
            this.Monitor.VerboseLog($"Context: load stage changed to {newStage}");

            // raise events
            this.Events.LoadStageChanged.Raise(new LoadStageChangedEventArgs(oldStage, newStage));
            if (newStage == LoadStage.None)
                this.Events.ReturnedToTitle.RaiseEmpty();
        }

        /// <summary>Constructor a content manager to read XNB files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        protected override LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            // Game1._temporaryContent initialising from SGame constructor
            // NOTE: this method is called before the SGame constructor runs. Don't depend on anything being initialised at this point.
            if (this.ContentCore == null)
            {
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, SGame.ConstructorHack.Monitor, SGame.ConstructorHack.Reflection, SGame.ConstructorHack.JsonHelper, this.OnLoadingFirstAsset ?? SGame.ConstructorHack?.OnLoadingFirstAsset);
                this.NextContentManagerIsMain = true;
                return this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
            }

            // Game1.content initialising from LoadContent
            if (this.NextContentManagerIsMain)
            {
                this.NextContentManagerIsMain = false;
                return this.ContentCore.MainContentManager;
            }

            // any other content manager
            return this.ContentCore.CreateGameContentManager("(generated)");
        }

        /// <summary>The method called when the game is updating its state. This happens roughly 60 times per second.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        protected override void Update(GameTime gameTime)
        {
            var events = this.Events;

            try
            {
                this.DeprecationManager.PrintQueued();

                /*********
                ** First-tick initialisation
                *********/
                if (!this.IsInitialised)
                {
                    this.IsInitialised = true;
                    this.InitialiseAfterGameStarted();
                }

                /*********
                ** Update input
                *********/
                // This should *always* run, even when suppressing mod events, since the game uses
                // this too. For example, doing this after mod event suppression would prevent the
                // user from doing anything on the overnight shipping screen.
                SInputState inputState = this.Input;
                if (this.IsActive)
                    inputState.TrueUpdate();

                /*********
                ** Special cases
                *********/
                // Abort if SMAPI is exiting.
                if (this.CancellationToken.IsCancellationRequested)
                {
                    this.Monitor.Log("SMAPI shutting down: aborting update.", LogLevel.Trace);
                    return;
                }

                // Run async tasks synchronously to avoid issues due to mod events triggering
                // concurrently with game code.
                bool saveParsed = false;
                if (Game1.currentLoader != null)
                {
                    this.Monitor.Log("Game loader synchronising...", LogLevel.Trace);
                    while (Game1.currentLoader?.MoveNext() == true)
                    {
                        // raise load stage changed
                        switch (Game1.currentLoader.Current)
                        {
                            case 20 when (!saveParsed && SaveGame.loaded != null):
                                saveParsed = true;
                                this.OnLoadStageChanged(LoadStage.SaveParsed);
                                break;

                            case 36:
                                this.OnLoadStageChanged(LoadStage.SaveLoadedBasicInfo);
                                break;

                            case 50:
                                this.OnLoadStageChanged(LoadStage.SaveLoadedLocations);
                                break;

                            default:
                                if (Game1.gameMode == Game1.playingGameMode)
                                    this.OnLoadStageChanged(LoadStage.Preloaded);
                                break;
                        }
                    }

                    Game1.currentLoader = null;
                    this.Monitor.Log("Game loader done.", LogLevel.Trace);
                }
                if (Game1._newDayTask?.Status == TaskStatus.Created)
                {
                    this.Monitor.Log("New day task synchronising...", LogLevel.Trace);
                    Game1._newDayTask.RunSynchronously();
                    this.Monitor.Log("New day task done.", LogLevel.Trace);
                }

                // While a background task is in progress, the game may make changes to the game
                // state while mods are running their code. This is risky, because data changes can
                // conflict (e.g. collection changed during enumeration errors) and data may change
                // unexpectedly from one mod instruction to the next.
                // 
                // Therefore we can just run Game1.Update here without raising any SMAPI events. There's
                // a small chance that the task will finish after we defer but before the game checks,
                // which means technically events should be raised, but the effects of missing one
                // update tick are neglible and not worth the complications of bypassing Game1.Update.
                if (Game1._newDayTask != null || Game1.gameMode == Game1.loadingMode)
                {
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    SGame.TicksElapsed++;
                    base.Update(gameTime);
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    return;
                }

                // Raise minimal events while saving.
                // While the game is writing to the save file in the background, mods can unexpectedly
                // fail since they don't have exclusive access to resources (e.g. collection changed
                // during enumeration errors). To avoid problems, events are not invoked while a save
                // is in progress. It's safe to raise SaveEvents.BeforeSave as soon as the menu is
                // opened (since the save hasn't started yet), but all other events should be suppressed.
                if (Context.IsSaving)
                {
                    // raise before-create
                    if (!Context.IsWorldReady && !this.IsBetweenCreateEvents)
                    {
                        this.IsBetweenCreateEvents = true;
                        this.Monitor.Log("Context: before save creation.", LogLevel.Trace);
                        events.SaveCreating.RaiseEmpty();
                    }

                    // raise before-save
                    if (Context.IsWorldReady && !this.IsBetweenSaveEvents)
                    {
                        this.IsBetweenSaveEvents = true;
                        this.Monitor.Log("Context: before save.", LogLevel.Trace);
                        events.Saving.RaiseEmpty();
                    }

                    // suppress non-save events
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    SGame.TicksElapsed++;
                    base.Update(gameTime);
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    return;
                }

                /*********
                ** Execute commands
                *********/
                while (this.CommandQueue.TryDequeue(out string rawInput))
                {
                    // parse command
                    string name;
                    string[] args;
                    Command command;
                    try
                    {
                        if (!this.CommandManager.TryParse(rawInput, out name, out args, out command))
                        {
                            this.Monitor.Log("Unknown command; type 'help' for a list of available commands.", LogLevel.Error);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"Failed parsing that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                        continue;
                    }

                    // execute command
                    try
                    {
                        command.Callback.Invoke(name, args);
                    }
                    catch (Exception ex)
                    {
                        if (command.Mod != null)
                            command.Mod.LogAsMod($"Mod failed handling that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                        else
                            this.Monitor.Log($"Failed handling that command:\n{ex.GetLogSummary()}", LogLevel.Error);
                    }
                }

                /*********
                ** Update context
                *********/
                bool wasWorldReady = Context.IsWorldReady;
                if ((Context.IsWorldReady && !Context.IsSaveLoaded) || Game1.exitToTitle)
                {
                    Context.IsWorldReady = false;
                    this.AfterLoadTimer.Reset();
                }
                else if (Context.IsSaveLoaded && this.AfterLoadTimer.Current > 0 && Game1.currentLocation != null)
                {
                    if (Game1.dayOfMonth != 0) // wait until new-game intro finishes (world not fully initialised yet)
                        this.AfterLoadTimer.Decrement();
                    Context.IsWorldReady = this.AfterLoadTimer.Current == 0;
                }

                /*********
                ** Update watchers
                **   (Watchers need to be updated, checked, and reset in one go so we can detect any changes mods make in event handlers.)
                *********/
                this.Watchers.Update();
                this.WatcherSnapshot.Update(this.Watchers);
                this.Watchers.Reset();
                WatcherSnapshot state = this.WatcherSnapshot;

                /*********
                ** Pre-update events
                *********/
                {
                    /*********
                    ** Save created/loaded events
                    *********/
                    if (this.IsBetweenCreateEvents)
                    {
                        // raise after-create
                        this.IsBetweenCreateEvents = false;
                        this.Monitor.Log($"Context: after save creation, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.", LogLevel.Trace);
                        this.OnLoadStageChanged(LoadStage.CreatedSaveFile);
                        events.SaveCreated.RaiseEmpty();
                    }
                    if (this.IsBetweenSaveEvents)
                    {
                        // raise after-save
                        this.IsBetweenSaveEvents = false;
                        this.Monitor.Log($"Context: after save, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.", LogLevel.Trace);
                        events.Saved.RaiseEmpty();
                        events.DayStarted.RaiseEmpty();
                    }

                    /*********
                    ** Locale changed events
                    *********/
                    if (state.Locale.IsChanged)
                        this.Monitor.Log($"Context: locale set to {state.Locale.New}.", LogLevel.Trace);

                    /*********
                    ** Load / return-to-title events
                    *********/
                    if (wasWorldReady && !Context.IsWorldReady)
                        this.OnLoadStageChanged(LoadStage.None);
                    else if (Context.IsWorldReady && Context.LoadStage != LoadStage.Ready)
                    {
                        // print context
                        string context = $"Context: loaded save '{Constants.SaveFolderName}', starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}, locale set to {this.ContentCore.Language}.";
                        if (Context.IsMultiplayer)
                        {
                            int onlineCount = Game1.getOnlineFarmers().Count();
                            context += $" {(Context.IsMainPlayer ? "Main player" : "Farmhand")} with {onlineCount} {(onlineCount == 1 ? "player" : "players")} online.";
                        }
                        else
                            context += " Single-player.";
                        this.Monitor.Log(context, LogLevel.Trace);

                        // raise events
                        this.OnLoadStageChanged(LoadStage.Ready);
                        events.SaveLoaded.RaiseEmpty();
                        events.DayStarted.RaiseEmpty();
                    }

                    /*********
                    ** Window events
                    *********/
                    // Here we depend on the game's viewport instead of listening to the Window.Resize
                    // event because we need to notify mods after the game handles the resize, so the
                    // game's metadata (like Game1.viewport) are updated. That's a bit complicated
                    // since the game adds & removes its own handler on the fly.
                    if (state.WindowSize.IsChanged)
                    {
                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Events: window size changed to {state.WindowSize.New}.", LogLevel.Trace);

                        events.WindowResized.Raise(new WindowResizedEventArgs(state.WindowSize.Old, state.WindowSize.New));
                    }

                    /*********
                    ** Input events (if window has focus)
                    *********/
                    if (this.IsActive)
                    {
                        // raise events
                        bool isChatInput = Game1.IsChatting || (Context.IsMultiplayer && Context.IsWorldReady && Game1.activeClickableMenu == null && Game1.currentMinigame == null && inputState.IsAnyDown(Game1.options.chatButton));
                        if (!isChatInput)
                        {
                            ICursorPosition cursor = this.Input.CursorPosition;

                            // raise cursor moved event
                            if (state.Cursor.IsChanged)
                                events.CursorMoved.Raise(new CursorMovedEventArgs(state.Cursor.Old, state.Cursor.New));

                            // raise mouse wheel scrolled
                            if (state.MouseWheelScroll.IsChanged)
                            {
                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Events: mouse wheel scrolled to {state.MouseWheelScroll.New}.", LogLevel.Trace);
                                events.MouseWheelScrolled.Raise(new MouseWheelScrolledEventArgs(cursor, state.MouseWheelScroll.Old, state.MouseWheelScroll.New));
                            }

                            // raise input button events
                            foreach (var pair in inputState.ActiveButtons)
                            {
                                SButton button = pair.Key;
                                InputStatus status = pair.Value;

                                if (status == InputStatus.Pressed)
                                {
                                    if (this.Monitor.IsVerbose)
                                        this.Monitor.Log($"Events: button {button} pressed.", LogLevel.Trace);

                                    events.ButtonPressed.Raise(new ButtonPressedEventArgs(button, cursor, inputState));
                                }
                                else if (status == InputStatus.Released)
                                {
                                    if (this.Monitor.IsVerbose)
                                        this.Monitor.Log($"Events: button {button} released.", LogLevel.Trace);

                                    events.ButtonReleased.Raise(new ButtonReleasedEventArgs(button, cursor, inputState));
                                }
                            }
                        }
                    }

                    /*********
                    ** Menu events
                    *********/
                    if (state.ActiveMenu.IsChanged)
                    {
                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Context: menu changed from {state.ActiveMenu.Old?.GetType().FullName ?? "none"} to {state.ActiveMenu.New?.GetType().FullName ?? "none"}.", LogLevel.Trace);

                        // raise menu events
                        events.MenuChanged.Raise(new MenuChangedEventArgs(state.ActiveMenu.Old, state.ActiveMenu.New));
                    }

                    /*********
                    ** World & player events
                    *********/
                    if (Context.IsWorldReady)
                    {
                        bool raiseWorldEvents = !state.SaveID.IsChanged; // don't report changes from unloaded => loaded

                        // location list changes
                        if (state.Locations.LocationList.IsChanged && (events.LocationListChanged.HasListeners() || this.Monitor.IsVerbose))
                        {
                            var added = state.Locations.LocationList.Added.ToArray();
                            var removed = state.Locations.LocationList.Removed.ToArray();

                            if (this.Monitor.IsVerbose)
                            {
                                string addedText = added.Any() ? string.Join(", ", added.Select(p => p.Name)) : "none";
                                string removedText = removed.Any() ? string.Join(", ", removed.Select(p => p.Name)) : "none";
                                this.Monitor.Log($"Context: location list changed (added {addedText}; removed {removedText}).", LogLevel.Trace);
                            }

                            events.LocationListChanged.Raise(new LocationListChangedEventArgs(added, removed));
                        }

                        // raise location contents changed
                        if (raiseWorldEvents)
                        {
                            foreach (LocationSnapshot locState in state.Locations.Locations)
                            {
                                var location = locState.Location;

                                // buildings changed
                                if (locState.Buildings.IsChanged)
                                    events.BuildingListChanged.Raise(new BuildingListChangedEventArgs(location, locState.Buildings.Added, locState.Buildings.Removed));

                                // debris changed
                                if (locState.Debris.IsChanged)
                                    events.DebrisListChanged.Raise(new DebrisListChangedEventArgs(location, locState.Debris.Added, locState.Debris.Removed));

                                // large terrain features changed
                                if (locState.LargeTerrainFeatures.IsChanged)
                                    events.LargeTerrainFeatureListChanged.Raise(new LargeTerrainFeatureListChangedEventArgs(location, locState.LargeTerrainFeatures.Added, locState.LargeTerrainFeatures.Removed));

                                // NPCs changed
                                if (locState.Npcs.IsChanged)
                                    events.NpcListChanged.Raise(new NpcListChangedEventArgs(location, locState.Npcs.Added, locState.Npcs.Removed));

                                // objects changed
                                if (locState.Objects.IsChanged)
                                    events.ObjectListChanged.Raise(new ObjectListChangedEventArgs(location, locState.Objects.Added, locState.Objects.Removed));

                                // terrain features changed
                                if (locState.TerrainFeatures.IsChanged)
                                    events.TerrainFeatureListChanged.Raise(new TerrainFeatureListChangedEventArgs(location, locState.TerrainFeatures.Added, locState.TerrainFeatures.Removed));
                            }
                        }

                        // raise time changed
                        if (raiseWorldEvents && state.Time.IsChanged)
                            events.TimeChanged.Raise(new TimeChangedEventArgs(state.Time.Old, state.Time.New));

                        // raise player events
                        if (raiseWorldEvents)
                        {
                            PlayerSnapshot playerState = state.CurrentPlayer;
                            Farmer player = playerState.Player;

                            // raise current location changed
                            if (playerState.Location.IsChanged)
                            {
                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Context: set location to {playerState.Location.New}.", LogLevel.Trace);

                                events.Warped.Raise(new WarpedEventArgs(player, playerState.Location.Old, playerState.Location.New));
                            }

                            // raise player leveled up a skill
                            foreach (var pair in playerState.Skills)
                            {
                                if (!pair.Value.IsChanged)
                                    continue;

                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Events: player skill '{pair.Key}' changed from {pair.Value.Old} to {pair.Value.New}.", LogLevel.Trace);

                                events.LevelChanged.Raise(new LevelChangedEventArgs(player, pair.Key, pair.Value.Old, pair.Value.New));
                            }

                            // raise player inventory changed
                            ItemStackChange[] changedItems = playerState.InventoryChanges.ToArray();
                            if (changedItems.Any())
                            {
                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log("Events: player inventory changed.", LogLevel.Trace);
                                events.InventoryChanged.Raise(new InventoryChangedEventArgs(player, changedItems));
                            }
                        }
                    }

                    /*********
                    ** Game update
                    *********/
                    // game launched
                    bool isFirstTick = SGame.TicksElapsed == 0;
                    if (isFirstTick)
                    {
                        Context.IsGameLaunched = true;
                        events.GameLaunched.Raise(new GameLaunchedEventArgs());
                    }

                    // preloaded
                    if (Context.IsSaveLoaded && Context.LoadStage != LoadStage.Loaded && Context.LoadStage != LoadStage.Ready && Game1.dayOfMonth != 0)
                        this.OnLoadStageChanged(LoadStage.Loaded);
                }

                /*********
                ** Game update tick
                *********/
                {
                    bool isOneSecond = SGame.TicksElapsed % 60 == 0;
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    events.UpdateTicking.RaiseEmpty();
                    if (isOneSecond)
                        events.OneSecondUpdateTicking.RaiseEmpty();
                    try
                    {
                        this.Input.UpdateSuppression();
                        SGame.TicksElapsed++;
                        base.Update(gameTime);
                    }
                    catch (Exception ex)
                    {
                        this.MonitorForGame.Log($"An error occured in the base update loop: {ex.GetLogSummary()}", LogLevel.Error);
                    }

                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    events.UpdateTicked.RaiseEmpty();
                    if (isOneSecond)
                        events.OneSecondUpdateTicked.RaiseEmpty();
                }

                /*********
                ** Update events
                *********/
                this.UpdateCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occured in the overridden update loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.UpdateCrashTimer.Decrement())
                    this.ExitGameImmediately("The game crashed when updating, and SMAPI was unable to recover the game.");
            }
        }

        /// <summary>The method called to draw everything to the screen.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        protected override void Draw(GameTime gameTime)
        {
            Context.IsInDrawLoop = true;
            try
            {
                this.DrawImpl(gameTime);
                this.DrawCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occured in the overridden draw loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.DrawCrashTimer.Decrement())
                {
                    this.ExitGameImmediately("The game crashed when drawing, and SMAPI was unable to recover the game.");
                    return;
                }

                // recover sprite batch
                try
                {
                    if (Game1.spriteBatch.IsOpen(this.Reflection))
                    {
                        this.Monitor.Log("Recovering sprite batch from error...", LogLevel.Trace);
                        Game1.spriteBatch.End();
                    }
                }
                catch (Exception innerEx)
                {
                    this.Monitor.Log($"Could not recover sprite batch state: {innerEx.GetLogSummary()}", LogLevel.Error);
                }
            }
            Context.IsInDrawLoop = false;
        }

        /// <summary>Replicate the game's draw logic with some changes for SMAPI.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        /// <remarks>This implementation is identical to <see cref="Game1.Draw"/>, except for try..catch around menu draw code, private field references replaced by wrappers, and added events.</remarks>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "LocalVariableHidesMember", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantCast", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantExplicitNullableCreation", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod", Justification = "copied from game code as-is")]
        [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField", Justification = "copied from game code as-is")]
        [SuppressMessage("SMAPI.CommonErrors", "AvoidImplicitNetFieldCast", Justification = "copied from game code as-is")]
        private void DrawImpl(GameTime gameTime)
        {
            var events = this.Events;

            if (Game1._newDayTask != null)
                this.GraphicsDevice.Clear(Game1.bgColor);
            else
            {
                if ((double)Game1.options.zoomLevel != 1.0)
                    this.GraphicsDevice.SetRenderTarget(this.screen);
                if (this.IsSaving)
                {
                    this.GraphicsDevice.Clear(Game1.bgColor);
                    IClickableMenu activeClickableMenu = Game1.activeClickableMenu;
                    if (activeClickableMenu != null)
                    {
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                        events.Rendering.RaiseEmpty();
                        try
                        {
                            events.RenderingActiveMenu.RaiseEmpty();
                            activeClickableMenu.draw(Game1.spriteBatch);
                            events.RenderedActiveMenu.RaiseEmpty();
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"The {activeClickableMenu.GetType().FullName} menu crashed while drawing itself during save. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                            activeClickableMenu.exitThisMenu();
                        }
                        events.Rendered.RaiseEmpty();
                        Game1.spriteBatch.End();
                    }
                    if (Game1.overlayMenu != null)
                    {
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                        Game1.overlayMenu.draw(Game1.spriteBatch);
                        Game1.spriteBatch.End();
                    }
                    this.renderScreenBuffer();
                }
                else
                {
                    this.GraphicsDevice.Clear(Game1.bgColor);
                    if (Game1.activeClickableMenu != null && Game1.options.showMenuBackground && Game1.activeClickableMenu.showWithoutTransparencyIfOptionIsSet())
                    {
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);

                        events.Rendering.RaiseEmpty();
                        try
                        {
                            Game1.activeClickableMenu.drawBackground(Game1.spriteBatch);
                            events.RenderingActiveMenu.RaiseEmpty();
                            Game1.activeClickableMenu.draw(Game1.spriteBatch);
                            events.RenderedActiveMenu.RaiseEmpty();
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                            Game1.activeClickableMenu.exitThisMenu();
                        }
                        events.Rendered.RaiseEmpty();
                        Game1.spriteBatch.End();
                        this.drawOverlays(Game1.spriteBatch);
                        if ((double)Game1.options.zoomLevel != 1.0)
                        {
                            this.GraphicsDevice.SetRenderTarget((RenderTarget2D)null);
                            this.GraphicsDevice.Clear(Game1.bgColor);
                            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                            Game1.spriteBatch.Draw((Texture2D)this.screen, Vector2.Zero, new Microsoft.Xna.Framework.Rectangle?(this.screen.Bounds), Color.White, 0.0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                            Game1.spriteBatch.End();
                        }
                        if (Game1.overlayMenu == null)
                            return;
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                        Game1.overlayMenu.draw(Game1.spriteBatch);
                        Game1.spriteBatch.End();
                    }
                    else if (Game1.gameMode == (byte)11)
                    {
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                        events.Rendering.RaiseEmpty();
                        Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3685"), new Vector2(16f, 16f), Color.HotPink);
                        Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3686"), new Vector2(16f, 32f), new Color(0, (int)byte.MaxValue, 0));
                        Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.parseText(Game1.errorMessage, Game1.dialogueFont, Game1.graphics.GraphicsDevice.Viewport.Width), new Vector2(16f, 48f), Color.White);
                        events.Rendered.RaiseEmpty();
                        Game1.spriteBatch.End();
                    }
                    else if (Game1.currentMinigame != null)
                    {
                        Game1.currentMinigame.draw(Game1.spriteBatch);
                        if (Game1.globalFade && !Game1.menuUp && (!Game1.nameSelectUp || Game1.messagePause))
                        {
                            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                            Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * (Game1.gameMode == (byte)0 ? 1f - Game1.fadeToBlackAlpha : Game1.fadeToBlackAlpha));
                            Game1.spriteBatch.End();
                        }
                        this.drawOverlays(Game1.spriteBatch);
                        if ((double)Game1.options.zoomLevel == 1.0)
                            return;
                        this.GraphicsDevice.SetRenderTarget((RenderTarget2D)null);
                        this.GraphicsDevice.Clear(Game1.bgColor);
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                        Game1.spriteBatch.Draw((Texture2D)this.screen, Vector2.Zero, new Microsoft.Xna.Framework.Rectangle?(this.screen.Bounds), Color.White, 0.0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                        Game1.spriteBatch.End();
                    }
                    else if (Game1.showingEndOfNightStuff)
                    {
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                        events.Rendering.RaiseEmpty();
                        if (Game1.activeClickableMenu != null)
                        {
                            try
                            {
                                events.RenderingActiveMenu.RaiseEmpty();
                                Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                events.RenderedActiveMenu.RaiseEmpty();
                            }
                            catch (Exception ex)
                            {
                                this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during end-of-night-stuff. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                Game1.activeClickableMenu.exitThisMenu();
                            }
                        }
                        events.Rendered.RaiseEmpty();
                        Game1.spriteBatch.End();
                        this.drawOverlays(Game1.spriteBatch);
                        if ((double)Game1.options.zoomLevel == 1.0)
                            return;
                        this.GraphicsDevice.SetRenderTarget((RenderTarget2D)null);
                        this.GraphicsDevice.Clear(Game1.bgColor);
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                        Game1.spriteBatch.Draw((Texture2D)this.screen, Vector2.Zero, new Microsoft.Xna.Framework.Rectangle?(this.screen.Bounds), Color.White, 0.0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                        Game1.spriteBatch.End();
                    }
                    else if (Game1.gameMode == (byte)6 || Game1.gameMode == (byte)3 && Game1.currentLocation == null)
                    {
                        Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                        events.Rendering.RaiseEmpty();
                        string str1 = "";
                        for (int index = 0; (double)index < gameTime.TotalGameTime.TotalMilliseconds % 999.0 / 333.0; ++index)
                            str1 += ".";
                        string str2 = Game1.content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3688");
                        string s = str2 + str1;
                        string str3 = str2 + "... ";
                        int widthOfString = SpriteText.getWidthOfString(str3, 999999);
                        int height = 64;
                        int x = 64;
                        int y = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - height;
                        SpriteText.drawString(Game1.spriteBatch, s, x, y, 999999, widthOfString, height, 1f, 0.88f, false, 0, str3, -1, SpriteText.ScrollTextAlignment.Left);
                        events.Rendered.RaiseEmpty();
                        Game1.spriteBatch.End();
                        this.drawOverlays(Game1.spriteBatch);
                        if ((double)Game1.options.zoomLevel != 1.0)
                        {
                            this.GraphicsDevice.SetRenderTarget((RenderTarget2D)null);
                            this.GraphicsDevice.Clear(Game1.bgColor);
                            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
                            Game1.spriteBatch.Draw((Texture2D)this.screen, Vector2.Zero, new Microsoft.Xna.Framework.Rectangle?(this.screen.Bounds), Color.White, 0.0f, Vector2.Zero, Game1.options.zoomLevel, SpriteEffects.None, 1f);
                            Game1.spriteBatch.End();
                        }
                        if (Game1.overlayMenu != null)
                        {
                            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                            Game1.overlayMenu.draw(Game1.spriteBatch);
                            Game1.spriteBatch.End();
                        }
                        //base.Draw(gameTime);
                    }
                    else
                    {
                        byte batchOpens = 0; // used for rendering event

                        Viewport viewport;
                        if (Game1.gameMode == (byte)0)
                        {
                            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                            if (++batchOpens == 1)
                                events.Rendering.RaiseEmpty();
                        }
                        else
                        {
                            if (Game1.drawLighting)
                            {
                                this.GraphicsDevice.SetRenderTarget(Game1.lightmap);
                                this.GraphicsDevice.Clear(Color.White * 0.0f);
                                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                                if (++batchOpens == 1)
                                    events.Rendering.RaiseEmpty();
                                Color color1 = !Game1.currentLocation.Name.StartsWith("UndergroundMine") || !(Game1.currentLocation is MineShaft) ? (Game1.ambientLight.Equals(Color.White) || Game1.isRaining && (bool)((NetFieldBase<bool, NetBool>)Game1.currentLocation.isOutdoors) ? Game1.outdoorLight : Game1.ambientLight) : (Game1.currentLocation as MineShaft).getLightingColor(gameTime);
                                Game1.spriteBatch.Draw(Game1.staminaRect, Game1.lightmap.Bounds, color1);
                                for (int index = 0; index < Game1.currentLightSources.Count; ++index)
                                {
                                    LightSource lightSource = Game1.currentLightSources.ElementAt<LightSource>(index);
                                    if (lightSource.PlayerID != 0L && lightSource.PlayerID != Game1.player.UniqueMultiplayerID)
                                    {
                                        Farmer farmerMaybeOffline = Game1.getFarmerMaybeOffline(lightSource.PlayerID);
                                        if (farmerMaybeOffline == null || farmerMaybeOffline.currentLocation != null && farmerMaybeOffline.currentLocation.Name != Game1.currentLocation.Name || (bool)((NetFieldBase<bool, NetBool>)farmerMaybeOffline.hidden))
                                            continue;
                                    }
                                    if (Utility.isOnScreen((Vector2)((NetFieldBase<Vector2, NetVector2>)Game1.currentLightSources.ElementAt<LightSource>(index).position), (int)((double)(float)((NetFieldBase<float, NetFloat>)Game1.currentLightSources.ElementAt<LightSource>(index).radius) * 64.0 * 4.0)))
                                    {
                                        SpriteBatch spriteBatch = Game1.spriteBatch;
                                        Texture2D lightTexture = Game1.currentLightSources.ElementAt<LightSource>(index).lightTexture;
                                        Vector2 position = Game1.GlobalToLocal(Game1.viewport, (Vector2)((NetFieldBase<Vector2, NetVector2>)Game1.currentLightSources.ElementAt<LightSource>(index).position)) / (float)(Game1.options.lightingQuality / 2);
                                        Microsoft.Xna.Framework.Rectangle? sourceRectangle = new Microsoft.Xna.Framework.Rectangle?(Game1.currentLightSources.ElementAt<LightSource>(index).lightTexture.Bounds);
                                        Color color2 = (Color)((NetFieldBase<Color, NetColor>)Game1.currentLightSources.ElementAt<LightSource>(index).color);
                                        Microsoft.Xna.Framework.Rectangle bounds = Game1.currentLightSources.ElementAt<LightSource>(index).lightTexture.Bounds;
                                        double x = (double)bounds.Center.X;
                                        bounds = Game1.currentLightSources.ElementAt<LightSource>(index).lightTexture.Bounds;
                                        double y = (double)bounds.Center.Y;
                                        Vector2 origin = new Vector2((float)x, (float)y);
                                        double num = (double)(float)((NetFieldBase<float, NetFloat>)Game1.currentLightSources.ElementAt<LightSource>(index).radius) / (double)(Game1.options.lightingQuality / 2);
                                        spriteBatch.Draw(lightTexture, position, sourceRectangle, color2, 0.0f, origin, (float)num, SpriteEffects.None, 0.9f);
                                    }
                                }
                                Game1.spriteBatch.End();
                                this.GraphicsDevice.SetRenderTarget((double)Game1.options.zoomLevel == 1.0 ? (RenderTarget2D)null : this.screen);
                            }
                            if (Game1.bloomDay && Game1.bloom != null)
                                Game1.bloom.BeginDraw();
                            this.GraphicsDevice.Clear(Game1.bgColor);
                            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                            if (++batchOpens == 1)
                                events.Rendering.RaiseEmpty();
                            events.RenderingWorld.RaiseEmpty();
                            if (Game1.background != null)
                                Game1.background.draw(Game1.spriteBatch);
                            Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                            Game1.currentLocation.Map.GetLayer("Back").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                            Game1.currentLocation.drawWater(Game1.spriteBatch);
                            this._farmerShadows.Clear();
                            if (Game1.currentLocation.currentEvent != null && !Game1.currentLocation.currentEvent.isFestival && Game1.currentLocation.currentEvent.farmerActors.Count > 0)
                            {
                                foreach (Farmer farmerActor in Game1.currentLocation.currentEvent.farmerActors)
                                {
                                    if (farmerActor.IsLocalPlayer && Game1.displayFarmer || !(bool)((NetFieldBase<bool, NetBool>)farmerActor.hidden))
                                        this._farmerShadows.Add(farmerActor);
                                }
                            }
                            else
                            {
                                foreach (Farmer farmer in Game1.currentLocation.farmers)
                                {
                                    if (farmer.IsLocalPlayer && Game1.displayFarmer || !(bool)((NetFieldBase<bool, NetBool>)farmer.hidden))
                                        this._farmerShadows.Add(farmer);
                                }
                            }
                            if (!Game1.currentLocation.shouldHideCharacters())
                            {
                                if (Game1.CurrentEvent == null)
                                {
                                    foreach (NPC character in Game1.currentLocation.characters)
                                    {
                                        if (!(bool)((NetFieldBase<bool, NetBool>)character.swimming) && !character.HideShadow && (!character.IsInvisible && !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(character.getTileLocation())))
                                            Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, character.Position + new Vector2((float)(character.Sprite.SpriteWidth * 4) / 2f, (float)(character.GetBoundingBox().Height + (character.IsMonster ? 0 : 12)))), new Microsoft.Xna.Framework.Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0.0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)(4.0 + (double)character.yJumpOffset / 40.0) * (float)((NetFieldBase<float, NetFloat>)character.scale), SpriteEffects.None, Math.Max(0.0f, (float)character.getStandingY() / 10000f) - 1E-06f);
                                    }
                                }
                                else
                                {
                                    foreach (NPC actor in Game1.CurrentEvent.actors)
                                    {
                                        if (!(bool)((NetFieldBase<bool, NetBool>)actor.swimming) && !actor.HideShadow && !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(actor.getTileLocation()))
                                            Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, actor.Position + new Vector2((float)(actor.Sprite.SpriteWidth * 4) / 2f, (float)(actor.GetBoundingBox().Height + (actor.IsMonster ? 0 : (actor.Sprite.SpriteHeight <= 16 ? -4 : 12))))), new Microsoft.Xna.Framework.Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0.0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)(4.0 + (double)actor.yJumpOffset / 40.0) * (float)((NetFieldBase<float, NetFloat>)actor.scale), SpriteEffects.None, Math.Max(0.0f, (float)actor.getStandingY() / 10000f) - 1E-06f);
                                    }
                                }
                                foreach (Farmer farmerShadow in this._farmerShadows)
                                {
                                    if (!Game1.multiplayer.isDisconnecting(farmerShadow.UniqueMultiplayerID) && !(bool)((NetFieldBase<bool, NetBool>)farmerShadow.swimming) && !farmerShadow.isRidingHorse() && (Game1.currentLocation == null || !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmerShadow.getTileLocation())))
                                    {
                                        SpriteBatch spriteBatch = Game1.spriteBatch;
                                        Texture2D shadowTexture = Game1.shadowTexture;
                                        Vector2 local = Game1.GlobalToLocal(farmerShadow.Position + new Vector2(32f, 24f));
                                        Microsoft.Xna.Framework.Rectangle? sourceRectangle = new Microsoft.Xna.Framework.Rectangle?(Game1.shadowTexture.Bounds);
                                        Color white = Color.White;
                                        Microsoft.Xna.Framework.Rectangle bounds = Game1.shadowTexture.Bounds;
                                        double x = (double)bounds.Center.X;
                                        bounds = Game1.shadowTexture.Bounds;
                                        double y = (double)bounds.Center.Y;
                                        Vector2 origin = new Vector2((float)x, (float)y);
                                        double num = 4.0 - (!farmerShadow.running && !farmerShadow.UsingTool || farmerShadow.FarmerSprite.currentAnimationIndex <= 1 ? 0.0 : (double)Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmerShadow.FarmerSprite.CurrentFrame]) * 0.5);
                                        spriteBatch.Draw(shadowTexture, local, sourceRectangle, white, 0.0f, origin, (float)num, SpriteEffects.None, 0.0f);
                                    }
                                }
                            }
                            Game1.currentLocation.Map.GetLayer("Buildings").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                            Game1.mapDisplayDevice.EndScene();
                            Game1.spriteBatch.End();
                            Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                            if (!Game1.currentLocation.shouldHideCharacters())
                            {
                                if (Game1.CurrentEvent == null)
                                {
                                    foreach (NPC character in Game1.currentLocation.characters)
                                    {
                                        if (!(bool)((NetFieldBase<bool, NetBool>)character.swimming) && !character.HideShadow && (!(bool)((NetFieldBase<bool, NetBool>)character.isInvisible) && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(character.getTileLocation())))
                                            Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, character.Position + new Vector2((float)(character.Sprite.SpriteWidth * 4) / 2f, (float)(character.GetBoundingBox().Height + (character.IsMonster ? 0 : 12)))), new Microsoft.Xna.Framework.Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0.0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)(4.0 + (double)character.yJumpOffset / 40.0) * (float)((NetFieldBase<float, NetFloat>)character.scale), SpriteEffects.None, Math.Max(0.0f, (float)character.getStandingY() / 10000f) - 1E-06f);
                                    }
                                }
                                else
                                {
                                    foreach (NPC actor in Game1.CurrentEvent.actors)
                                    {
                                        if (!(bool)((NetFieldBase<bool, NetBool>)actor.swimming) && !actor.HideShadow && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(actor.getTileLocation()))
                                            Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, actor.Position + new Vector2((float)(actor.Sprite.SpriteWidth * 4) / 2f, (float)(actor.GetBoundingBox().Height + (actor.IsMonster ? 0 : 12)))), new Microsoft.Xna.Framework.Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0.0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)(4.0 + (double)actor.yJumpOffset / 40.0) * (float)((NetFieldBase<float, NetFloat>)actor.scale), SpriteEffects.None, Math.Max(0.0f, (float)actor.getStandingY() / 10000f) - 1E-06f);
                                    }
                                }
                                foreach (Farmer farmerShadow in this._farmerShadows)
                                {
                                    if (!(bool)((NetFieldBase<bool, NetBool>)farmerShadow.swimming) && !farmerShadow.isRidingHorse() && (Game1.currentLocation != null && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmerShadow.getTileLocation())))
                                    {
                                        SpriteBatch spriteBatch = Game1.spriteBatch;
                                        Texture2D shadowTexture = Game1.shadowTexture;
                                        Vector2 local = Game1.GlobalToLocal(farmerShadow.Position + new Vector2(32f, 24f));
                                        Microsoft.Xna.Framework.Rectangle? sourceRectangle = new Microsoft.Xna.Framework.Rectangle?(Game1.shadowTexture.Bounds);
                                        Color white = Color.White;
                                        Microsoft.Xna.Framework.Rectangle bounds = Game1.shadowTexture.Bounds;
                                        double x = (double)bounds.Center.X;
                                        bounds = Game1.shadowTexture.Bounds;
                                        double y = (double)bounds.Center.Y;
                                        Vector2 origin = new Vector2((float)x, (float)y);
                                        double num = 4.0 - (!farmerShadow.running && !farmerShadow.UsingTool || farmerShadow.FarmerSprite.currentAnimationIndex <= 1 ? 0.0 : (double)Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmerShadow.FarmerSprite.CurrentFrame]) * 0.5);
                                        spriteBatch.Draw(shadowTexture, local, sourceRectangle, white, 0.0f, origin, (float)num, SpriteEffects.None, 0.0f);
                                    }
                                }
                            }
                            if ((Game1.eventUp || Game1.killScreen) && (!Game1.killScreen && Game1.currentLocation.currentEvent != null))
                                Game1.currentLocation.currentEvent.draw(Game1.spriteBatch);
                            if (Game1.player.currentUpgrade != null && Game1.player.currentUpgrade.daysLeftTillUpgradeDone <= 3 && Game1.currentLocation.Name.Equals("Farm"))
                                Game1.spriteBatch.Draw(Game1.player.currentUpgrade.workerTexture, Game1.GlobalToLocal(Game1.viewport, Game1.player.currentUpgrade.positionOfCarpenter), new Microsoft.Xna.Framework.Rectangle?(Game1.player.currentUpgrade.getSourceRectangle()), Color.White, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, (float)(((double)Game1.player.currentUpgrade.positionOfCarpenter.Y + 48.0) / 10000.0));
                            Game1.currentLocation.draw(Game1.spriteBatch);
                            if (Game1.eventUp && Game1.currentLocation.currentEvent != null)
                            {
                                string messageToScreen = Game1.currentLocation.currentEvent.messageToScreen;
                            }
                            if (Game1.player.ActiveObject == null && (Game1.player.UsingTool || Game1.pickingTool) && (Game1.player.CurrentTool != null && (!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool)))
                                Game1.drawTool(Game1.player);
                            if (Game1.currentLocation.Name.Equals("Farm"))
                                this.drawFarmBuildings();
                            if (Game1.tvStation >= 0)
                                Game1.spriteBatch.Draw(Game1.tvStationTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(400f, 160f)), new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(Game1.tvStation * 24, 0, 24, 15)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-08f);
                            if (Game1.panMode)
                            {
                                Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((int)Math.Floor((double)(Game1.getOldMouseX() + Game1.viewport.X) / 64.0) * 64 - Game1.viewport.X, (int)Math.Floor((double)(Game1.getOldMouseY() + Game1.viewport.Y) / 64.0) * 64 - Game1.viewport.Y, 64, 64), Color.Lime * 0.75f);
                                foreach (Warp warp in (NetList<Warp, NetRef<Warp>>)Game1.currentLocation.warps)
                                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(warp.X * 64 - Game1.viewport.X, warp.Y * 64 - Game1.viewport.Y, 64, 64), Color.Red * 0.75f);
                            }
                            Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                            Game1.currentLocation.Map.GetLayer("Front").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                            Game1.mapDisplayDevice.EndScene();
                            Game1.currentLocation.drawAboveFrontLayer(Game1.spriteBatch);
                            Game1.spriteBatch.End();
                            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                            if (Game1.displayFarmer && Game1.player.ActiveObject != null && ((bool)((NetFieldBase<bool, NetBool>)Game1.player.ActiveObject.bigCraftable) && this.checkBigCraftableBoundariesForFrontLayer()) && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null)
                                Game1.drawPlayerHeldObject(Game1.player);
                            else if (Game1.displayFarmer && Game1.player.ActiveObject != null && (Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int)Game1.player.Position.X, (int)Game1.player.Position.Y - 38), Game1.viewport.Size) != null && !Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int)Game1.player.Position.X, (int)Game1.player.Position.Y - 38), Game1.viewport.Size).TileIndexProperties.ContainsKey("FrontAlways") || Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.GetBoundingBox().Right, (int)Game1.player.Position.Y - 38), Game1.viewport.Size) != null && !Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.GetBoundingBox().Right, (int)Game1.player.Position.Y - 38), Game1.viewport.Size).TileIndexProperties.ContainsKey("FrontAlways")))
                                Game1.drawPlayerHeldObject(Game1.player);
                            if ((Game1.player.UsingTool || Game1.pickingTool) && Game1.player.CurrentTool != null && ((!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool) && (Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), (int)Game1.player.Position.Y - 38), Game1.viewport.Size) != null && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null)))
                                Game1.drawTool(Game1.player);
                            if (Game1.currentLocation.Map.GetLayer("AlwaysFront") != null)
                            {
                                Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                                Game1.currentLocation.Map.GetLayer("AlwaysFront").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                Game1.mapDisplayDevice.EndScene();
                            }
                            if ((double)Game1.toolHold > 400.0 && Game1.player.CurrentTool.UpgradeLevel >= 1 && Game1.player.canReleaseTool)
                            {
                                Color color = Color.White;
                                switch ((int)((double)Game1.toolHold / 600.0) + 2)
                                {
                                    case 1:
                                        color = Tool.copperColor;
                                        break;
                                    case 2:
                                        color = Tool.steelColor;
                                        break;
                                    case 3:
                                        color = Tool.goldColor;
                                        break;
                                    case 4:
                                        color = Tool.iridiumColor;
                                        break;
                                }
                                Game1.spriteBatch.Draw(Game1.littleEffect, new Microsoft.Xna.Framework.Rectangle((int)Game1.player.getLocalPosition(Game1.viewport).X - 2, (int)Game1.player.getLocalPosition(Game1.viewport).Y - (Game1.player.CurrentTool.Name.Equals("Watering Can") ? 0 : 64) - 2, (int)((double)Game1.toolHold % 600.0 * 0.0799999982118607) + 4, 12), Color.Black);
                                Game1.spriteBatch.Draw(Game1.littleEffect, new Microsoft.Xna.Framework.Rectangle((int)Game1.player.getLocalPosition(Game1.viewport).X, (int)Game1.player.getLocalPosition(Game1.viewport).Y - (Game1.player.CurrentTool.Name.Equals("Watering Can") ? 0 : 64), (int)((double)Game1.toolHold % 600.0 * 0.0799999982118607), 8), color);
                            }
                            if (Game1.isDebrisWeather && Game1.currentLocation.IsOutdoors && (!(bool)((NetFieldBase<bool, NetBool>)Game1.currentLocation.ignoreDebrisWeather) && !Game1.currentLocation.Name.Equals("Desert")) && Game1.viewport.X > -10)
                            {
                                foreach (WeatherDebris weatherDebris in Game1.debrisWeather)
                                    weatherDebris.draw(Game1.spriteBatch);
                            }
                            if (Game1.farmEvent != null)
                                Game1.farmEvent.draw(Game1.spriteBatch);
                            if ((double)Game1.currentLocation.LightLevel > 0.0 && Game1.timeOfDay < 2000)
                            {
                                SpriteBatch spriteBatch = Game1.spriteBatch;
                                Texture2D fadeToBlackRect = Game1.fadeToBlackRect;
                                viewport = Game1.graphics.GraphicsDevice.Viewport;
                                Microsoft.Xna.Framework.Rectangle bounds = viewport.Bounds;
                                Color color = Color.Black * Game1.currentLocation.LightLevel;
                                spriteBatch.Draw(fadeToBlackRect, bounds, color);
                            }
                            if (Game1.screenGlow)
                            {
                                SpriteBatch spriteBatch = Game1.spriteBatch;
                                Texture2D fadeToBlackRect = Game1.fadeToBlackRect;
                                viewport = Game1.graphics.GraphicsDevice.Viewport;
                                Microsoft.Xna.Framework.Rectangle bounds = viewport.Bounds;
                                Color color = Game1.screenGlowColor * Game1.screenGlowAlpha;
                                spriteBatch.Draw(fadeToBlackRect, bounds, color);
                            }
                            Game1.currentLocation.drawAboveAlwaysFrontLayer(Game1.spriteBatch);
                            if (Game1.player.CurrentTool != null && Game1.player.CurrentTool is FishingRod && ((Game1.player.CurrentTool as FishingRod).isTimingCast || (double)(Game1.player.CurrentTool as FishingRod).castingChosenCountdown > 0.0 || ((Game1.player.CurrentTool as FishingRod).fishCaught || (Game1.player.CurrentTool as FishingRod).showingTreasure)))
                                Game1.player.CurrentTool.draw(Game1.spriteBatch);
                            if (Game1.isRaining && Game1.currentLocation.IsOutdoors && (!Game1.currentLocation.Name.Equals("Desert") && !(Game1.currentLocation is Summit)) && (!Game1.eventUp || Game1.currentLocation.isTileOnMap(new Vector2((float)(Game1.viewport.X / 64), (float)(Game1.viewport.Y / 64)))))
                            {
                                for (int index = 0; index < Game1.rainDrops.Length; ++index)
                                    Game1.spriteBatch.Draw(Game1.rainTexture, Game1.rainDrops[index].position, new Microsoft.Xna.Framework.Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.rainTexture, Game1.rainDrops[index].frame, -1, -1)), Color.White);
                            }
                            Game1.spriteBatch.End();
                            Game1.spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                            if (Game1.eventUp && Game1.currentLocation.currentEvent != null)
                            {
                                foreach (NPC actor in Game1.currentLocation.currentEvent.actors)
                                {
                                    if (actor.isEmoting)
                                    {
                                        Vector2 localPosition = actor.getLocalPosition(Game1.viewport);
                                        localPosition.Y -= 140f;
                                        if (actor.Age == 2)
                                            localPosition.Y += 32f;
                                        else if (actor.Gender == 1)
                                            localPosition.Y += 10f;
                                        Game1.spriteBatch.Draw(Game1.emoteSpriteSheet, localPosition, new Microsoft.Xna.Framework.Rectangle?(new Microsoft.Xna.Framework.Rectangle(actor.CurrentEmoteIndex * 16 % Game1.emoteSpriteSheet.Width, actor.CurrentEmoteIndex * 16 / Game1.emoteSpriteSheet.Width * 16, 16, 16)), Color.White, 0.0f, Vector2.Zero, 4f, SpriteEffects.None, (float)actor.getStandingY() / 10000f);
                                    }
                                }
                            }
                            Game1.spriteBatch.End();
                            if (Game1.drawLighting)
                            {
                                Game1.spriteBatch.Begin(SpriteSortMode.Deferred, this.lightingBlend, SamplerState.LinearClamp, (DepthStencilState)null, (RasterizerState)null);
                                Game1.spriteBatch.Draw((Texture2D)Game1.lightmap, Vector2.Zero, new Microsoft.Xna.Framework.Rectangle?(Game1.lightmap.Bounds), Color.White, 0.0f, Vector2.Zero, (float)(Game1.options.lightingQuality / 2), SpriteEffects.None, 1f);
                                if (Game1.isRaining && (bool)((NetFieldBase<bool, NetBool>)Game1.currentLocation.isOutdoors) && !(Game1.currentLocation is Desert))
                                {
                                    SpriteBatch spriteBatch = Game1.spriteBatch;
                                    Texture2D staminaRect = Game1.staminaRect;
                                    viewport = Game1.graphics.GraphicsDevice.Viewport;
                                    Microsoft.Xna.Framework.Rectangle bounds = viewport.Bounds;
                                    Color color = Color.OrangeRed * 0.45f;
                                    spriteBatch.Draw(staminaRect, bounds, color);
                                }
                                Game1.spriteBatch.End();
                            }
                            Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, (DepthStencilState)null, (RasterizerState)null);
                            events.RenderedWorld.RaiseEmpty();
                            if (Game1.drawGrid)
                            {
                                int num1 = -Game1.viewport.X % 64;
                                float num2 = (float)(-Game1.viewport.Y % 64);
                                int num3 = num1;
                                while (true)
                                {
                                    int num4 = num3;
                                    viewport = Game1.graphics.GraphicsDevice.Viewport;
                                    int width = viewport.Width;
                                    if (num4 < width)
                                    {
                                        SpriteBatch spriteBatch = Game1.spriteBatch;
                                        Texture2D staminaRect = Game1.staminaRect;
                                        int x = num3;
                                        int y = (int)num2;
                                        viewport = Game1.graphics.GraphicsDevice.Viewport;
                                        int height = viewport.Height;
                                        Microsoft.Xna.Framework.Rectangle destinationRectangle = new Microsoft.Xna.Framework.Rectangle(x, y, 1, height);
                                        Color color = Color.Red * 0.5f;
                                        spriteBatch.Draw(staminaRect, destinationRectangle, color);
                                        num3 += 64;
                                    }
                                    else
                                        break;
                                }
                                float num5 = num2;
                                while (true)
                                {
                                    double num4 = (double)num5;
                                    viewport = Game1.graphics.GraphicsDevice.Viewport;
                                    double height = (double)viewport.Height;
                                    if (num4 < height)
                                    {
                                        SpriteBatch spriteBatch = Game1.spriteBatch;
                                        Texture2D staminaRect = Game1.staminaRect;
                                        int x = num1;
                                        int y = (int)num5;
                                        viewport = Game1.graphics.GraphicsDevice.Viewport;
                                        int width = viewport.Width;
                                        Microsoft.Xna.Framework.Rectangle destinationRectangle = new Microsoft.Xna.Framework.Rectangle(x, y, width, 1);
                                        Color color = Color.Red * 0.5f;
                                        spriteBatch.Draw(staminaRect, destinationRectangle, color);
                                        num5 += 64f;
                                    }
                                    else
                                        break;
                                }
                            }
                            if (Game1.currentBillboard != 0)
                                this.drawBillboard();
                            if (!Game1.eventUp && Game1.farmEvent == null && (Game1.currentBillboard == 0 && Game1.gameMode == (byte)3) && Game1.isOutdoorMapSmallerThanViewport())
                            {
                                SpriteBatch spriteBatch1 = Game1.spriteBatch;
                                Texture2D fadeToBlackRect1 = Game1.fadeToBlackRect;
                                int width1 = -Math.Min(Game1.viewport.X, 4096);
                                viewport = Game1.graphics.GraphicsDevice.Viewport;
                                int height1 = viewport.Height;
                                Microsoft.Xna.Framework.Rectangle destinationRectangle1 = new Microsoft.Xna.Framework.Rectangle(0, 0, width1, height1);
                                Color black1 = Color.Black;
                                spriteBatch1.Draw(fadeToBlackRect1, destinationRectangle1, black1);
                                SpriteBatch spriteBatch2 = Game1.spriteBatch;
                                Texture2D fadeToBlackRect2 = Game1.fadeToBlackRect;
                                int x = -Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64;
                                viewport = Game1.graphics.GraphicsDevice.Viewport;
                                int width2 = Math.Min(4096, viewport.Width - (-Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64));
                                viewport = Game1.graphics.GraphicsDevice.Viewport;
                                int height2 = viewport.Height;
                                Microsoft.Xna.Framework.Rectangle destinationRectangle2 = new Microsoft.Xna.Framework.Rectangle(x, 0, width2, height2);
                                Color black2 = Color.Black;
                                spriteBatch2.Draw(fadeToBlackRect2, destinationRectangle2, black2);
                            }
                            if ((Game1.displayHUD || Game1.eventUp) && (Game1.currentBillboard == 0 && Game1.gameMode == (byte)3) && (!Game1.freezeControls && !Game1.panMode && !Game1.HostPaused))
                            {
                                events.RenderingHud.RaiseEmpty();
                                this.drawHUD();
                                events.RenderedHud.RaiseEmpty();
                            }
                            else if (Game1.activeClickableMenu == null)
                            {
                                FarmEvent farmEvent = Game1.farmEvent;
                            }
                            if (Game1.hudMessages.Count > 0)
                            {
                                for (int i = Game1.hudMessages.Count - 1; i >= 0; --i)
                                    Game1.hudMessages[i].draw(Game1.spriteBatch, i);
                            }
                        }
                        if (Game1.farmEvent != null)
                            Game1.farmEvent.draw(Game1.spriteBatch);
                        if (Game1.dialogueUp && !Game1.nameSelectUp && !Game1.messagePause && (Game1.activeClickableMenu == null || !(Game1.activeClickableMenu is DialogueBox)))
                            this.drawDialogueBox();
                        if (Game1.progressBar)
                        {
                            Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 128, Game1.dialogueWidth, 32), Color.LightGray);
                            Game1.spriteBatch.Draw(Game1.staminaRect, new Microsoft.Xna.Framework.Rectangle((Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 128, (int)((double)Game1.pauseAccumulator / (double)Game1.pauseTime * (double)Game1.dialogueWidth), 32), Color.DimGray);
                        }
                        if (Game1.eventUp && Game1.currentLocation != null && Game1.currentLocation.currentEvent != null)
                            Game1.currentLocation.currentEvent.drawAfterMap(Game1.spriteBatch);
                        if (Game1.isRaining && Game1.currentLocation != null && ((bool)((NetFieldBase<bool, NetBool>)Game1.currentLocation.isOutdoors) && !(Game1.currentLocation is Desert)))
                        {
                            SpriteBatch spriteBatch = Game1.spriteBatch;
                            Texture2D staminaRect = Game1.staminaRect;
                            viewport = Game1.graphics.GraphicsDevice.Viewport;
                            Microsoft.Xna.Framework.Rectangle bounds = viewport.Bounds;
                            Color color = Color.Blue * 0.2f;
                            spriteBatch.Draw(staminaRect, bounds, color);
                        }
                        if ((Game1.fadeToBlack || Game1.globalFade) && !Game1.menuUp && (!Game1.nameSelectUp || Game1.messagePause))
                        {
                            SpriteBatch spriteBatch = Game1.spriteBatch;
                            Texture2D fadeToBlackRect = Game1.fadeToBlackRect;
                            viewport = Game1.graphics.GraphicsDevice.Viewport;
                            Microsoft.Xna.Framework.Rectangle bounds = viewport.Bounds;
                            Color color = Color.Black * (Game1.gameMode == (byte)0 ? 1f - Game1.fadeToBlackAlpha : Game1.fadeToBlackAlpha);
                            spriteBatch.Draw(fadeToBlackRect, bounds, color);
                        }
                        else if ((double)Game1.flashAlpha > 0.0)
                        {
                            if (Game1.options.screenFlash)
                            {
                                SpriteBatch spriteBatch = Game1.spriteBatch;
                                Texture2D fadeToBlackRect = Game1.fadeToBlackRect;
                                viewport = Game1.graphics.GraphicsDevice.Viewport;
                                Microsoft.Xna.Framework.Rectangle bounds = viewport.Bounds;
                                Color color = Color.White * Math.Min(1f, Game1.flashAlpha);
                                spriteBatch.Draw(fadeToBlackRect, bounds, color);
                            }
                            Game1.flashAlpha -= 0.1f;
                        }
                        if ((Game1.messagePause || Game1.globalFade) && Game1.dialogueUp)
                            this.drawDialogueBox();
                        foreach (TemporaryAnimatedSprite overlayTempSprite in Game1.screenOverlayTempSprites)
                            overlayTempSprite.draw(Game1.spriteBatch, true, 0, 0, 1f);
                        if (Game1.debugMode)
                        {
                            StringBuilder debugStringBuilder = Game1._debugStringBuilder;
                            debugStringBuilder.Clear();
                            if (Game1.panMode)
                            {
                                debugStringBuilder.Append((Game1.getOldMouseX() + Game1.viewport.X) / 64);
                                debugStringBuilder.Append(",");
                                debugStringBuilder.Append((Game1.getOldMouseY() + Game1.viewport.Y) / 64);
                            }
                            else
                            {
                                debugStringBuilder.Append("player: ");
                                debugStringBuilder.Append(Game1.player.getStandingX() / 64);
                                debugStringBuilder.Append(", ");
                                debugStringBuilder.Append(Game1.player.getStandingY() / 64);
                            }
                            debugStringBuilder.Append(" mouseTransparency: ");
                            debugStringBuilder.Append(Game1.mouseCursorTransparency);
                            debugStringBuilder.Append(" mousePosition: ");
                            debugStringBuilder.Append(Game1.getMouseX());
                            debugStringBuilder.Append(",");
                            debugStringBuilder.Append(Game1.getMouseY());
                            debugStringBuilder.Append(Environment.NewLine);
                            debugStringBuilder.Append(" mouseWorldPosition: ");
                            debugStringBuilder.Append(Game1.getMouseX() + Game1.viewport.X);
                            debugStringBuilder.Append(",");
                            debugStringBuilder.Append(Game1.getMouseY() + Game1.viewport.Y);
                            debugStringBuilder.Append("  debugOutput: ");
                            debugStringBuilder.Append(Game1.debugOutput);
                            Game1.spriteBatch.DrawString(Game1.smallFont, debugStringBuilder, new Vector2((float)this.GraphicsDevice.Viewport.GetTitleSafeArea().X, (float)(this.GraphicsDevice.Viewport.GetTitleSafeArea().Y + Game1.smallFont.LineSpacing * 8)), Color.Red, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
                        }
                        if (Game1.showKeyHelp)
                            Game1.spriteBatch.DrawString(Game1.smallFont, Game1.keyHelpString, new Vector2(64f, (float)(Game1.viewport.Height - 64 - (Game1.dialogueUp ? 192 + (Game1.isQuestion ? Game1.questionChoices.Count * 64 : 0) : 0)) - Game1.smallFont.MeasureString(Game1.keyHelpString).Y), Color.LightGray, 0.0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
                        if (Game1.activeClickableMenu != null)
                        {
                            try
                            {
                                events.RenderingActiveMenu.RaiseEmpty();
                                Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                events.RenderedActiveMenu.RaiseEmpty();
                            }
                            catch (Exception ex)
                            {
                                this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                Game1.activeClickableMenu.exitThisMenu();
                            }
                        }
                        else if (Game1.farmEvent != null)
                            Game1.farmEvent.drawAboveEverything(Game1.spriteBatch);
                        if (Game1.emoteMenu != null)
                            Game1.emoteMenu.draw(Game1.spriteBatch);
                        if (Game1.HostPaused)
                        {
                            string s = Game1.content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10378");
                            SpriteText.drawStringWithScrollBackground(Game1.spriteBatch, s, 96, 32, "", 1f, -1, SpriteText.ScrollTextAlignment.Left);
                        }

                        events.Rendered.RaiseEmpty();
                        Game1.spriteBatch.End();
                        this.drawOverlays(Game1.spriteBatch);
                        this.renderScreenBuffer();
                    }
                }
            }
        }

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        /// <param name="message">The fatal log message.</param>
        private void ExitGameImmediately(string message)
        {
            this.Monitor.LogFatal(message);
            this.CancellationToken.Cancel();
        }
    }
}
