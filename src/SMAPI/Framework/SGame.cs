using System;
using System.Collections;
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
using StardewModdingAPI.Framework.Content;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.Networking;
using StardewModdingAPI.Framework.PerformanceMonitoring;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewModdingAPI.Framework.StateTracking.Snapshots;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Toolkit.Serialization;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Events;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Dimensions;
using xTile.Layers;
using xTile.ObjectModel;
using SObject = StardewValley.Object;
using xTile.Tiles;

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

        /// <summary>Tracks performance metrics.</summary>
        private readonly PerformanceMonitor PerformanceMonitor;

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from a draw error.</summary>
        private readonly Countdown DrawCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>The maximum number of consecutive attempts SMAPI should make to recover from an update error.</summary>
        private readonly Countdown UpdateCrashTimer = new Countdown(60); // 60 ticks = roughly one second

        /// <summary>The number of ticks until SMAPI should notify mods that the game has loaded.</summary>
        /// <remarks>Skipping a few frames ensures the game finishes initializing the world before mods try to change it.</remarks>
        private readonly Countdown AfterLoadTimer = new Countdown(5);

        /// <summary>Whether custom content was removed from the save data to avoid a crash.</summary>
        private bool IsSaveContentRemoved;

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        private bool IsBetweenSaveEvents;

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        private bool IsBetweenCreateEvents;

        /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
        private readonly Action OnLoadingFirstAsset;

        /// <summary>A callback to invoke after the game finishes initializing.</summary>
        private readonly Action OnGameInitialized;

        /// <summary>A callback to invoke when the game exits.</summary>
        private readonly Action OnGameExiting;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Encapsulates access to SMAPI core translations.</summary>
        private readonly Translator Translator;

        /// <summary>Propagates notification that SMAPI should exit.</summary>
        private readonly CancellationTokenSource CancellationToken;


        /****
        ** Game state
        ****/
        /// <summary>Monitors the entire game state for changes.</summary>
        private WatcherCore Watchers;

        /// <summary>A snapshot of the current <see cref="Watchers"/> state.</summary>
        private readonly WatcherSnapshot WatcherSnapshot = new WatcherSnapshot();

        /// <summary>Whether post-game-startup initialization has been performed.</summary>
        private bool IsInitialized;

        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;


        /*********
        ** Accessors
        *********/
        /// <summary>Static state to use while <see cref="Game1"/> is initializing, which happens before the <see cref="SGame"/> constructor runs.</summary>
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

        public static SGame instance;

        /// <summary>Asset interceptors added or removed since the last tick.</summary>
        private readonly List<AssetInterceptorChange> ReloadAssetInterceptorsQueue = new List<AssetInterceptorChange>();

        public bool IsGameSuspended;

        public bool IsAfterInitialize = false;
        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging for SMAPI.</param>
        /// <param name="monitorForGame">Encapsulates monitoring and logging on the game's behalf.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="translator">Encapsulates access to arbitrary translations.</param>
        /// <param name="eventManager">Manages SMAPI events for mods.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        /// <param name="modRegistry">Tracks the installed mods.</param>
        /// <param name="deprecationManager">Manages deprecation warnings.</param>
        /// <param name="performanceMonitor">Tracks performance metrics.</param>
        /// <param name="onGameInitialized">A callback to invoke after the game finishes initializing.</param>
        /// <param name="onGameExiting">A callback to invoke when the game exits.</param>
        /// <param name="cancellationToken">Propagates notification that SMAPI should exit.</param>
        /// <param name="logNetworkTraffic">Whether to log network traffic.</param>
        internal SGame(Monitor monitor, IMonitor monitorForGame, Reflector reflection, Translator translator, EventManager eventManager, JsonHelper jsonHelper, ModRegistry modRegistry, DeprecationManager deprecationManager, PerformanceMonitor performanceMonitor, Action onGameInitialized, Action onGameExiting, CancellationTokenSource cancellationToken, bool logNetworkTraffic)
        {
            this.OnLoadingFirstAsset = SGame.ConstructorHack.OnLoadingFirstAsset;
            SGame.ConstructorHack = null;

            // check expectations
            if (this.ContentCore == null)
                throw new InvalidOperationException($"The game didn't initialize its first content manager before SMAPI's {nameof(SGame)} constructor. This indicates an incompatible lifecycle change.");

            // init XNA
            Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // init SMAPI
            this.Monitor = monitor;
            this.MonitorForGame = monitorForGame;
            this.Events = eventManager;
            this.ModRegistry = modRegistry;
            this.Reflection = reflection;
            this.Translator = translator;
            this.DeprecationManager = deprecationManager;
            this.PerformanceMonitor = performanceMonitor;
            this.OnGameInitialized = onGameInitialized;
            this.OnGameExiting = onGameExiting;
            Game1.input = new SInputState();
            Game1.multiplayer = new SMultiplayer(monitor, eventManager, jsonHelper, modRegistry, reflection, this.OnModMessageReceived, logNetworkTraffic);
            Game1.hooks = new SModHooks(this.OnNewDayAfterFade);
            this.CancellationToken = cancellationToken;

            // init observables
            Game1.locations = new ObservableCollection<GameLocation>();
            SGame.instance = this;
        }

        /// <summary>Initialize just before the game's first update tick.</summary>
        private void InitializeAfterGameStarted()
        {
            // set initial state
            this.Input.TrueUpdate();

            // init watchers
            this.Watchers = new WatcherCore(this.Input);

            // raise callback
            this.OnGameInitialized();
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

        /// <summary>A callback invoked when custom content is removed from the save data to avoid a crash.</summary>
        internal void OnSaveContentRemoved()
        {
            this.IsSaveContentRemoved = true;
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

        /// <summary>A callback invoked when a mod adds or removes an asset interceptor.</summary>
        /// <param name="mod">The mod which added or removed interceptors.</param>
        /// <param name="added">The added interceptors.</param>
        /// <param name="removed">The removed interceptors.</param>
        internal void OnAssetInterceptorsChanged(IModMetadata mod, IEnumerable added, IEnumerable removed)
        {
            if (added != null)
            {
                foreach (object instance in added)
                    this.ReloadAssetInterceptorsQueue.Add(new AssetInterceptorChange(mod, instance, wasAdded: true));
            }
            if (removed != null)
            {
                foreach (object instance in removed)
                    this.ReloadAssetInterceptorsQueue.Add(new AssetInterceptorChange(mod, instance, wasAdded: false));
            }
        }

        /// <summary>Constructor a content manager to read XNB files.</summary>
        /// <param name="serviceProvider">The service provider to use to locate services.</param>
        /// <param name="rootDirectory">The root directory to search for content.</param>
        protected override LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            // Game1._temporaryContent initializing from SGame constructor
            // NOTE: this method is called before the SGame constructor runs. Don't depend on anything being initialized at this point.
            if (this.ContentCore == null)
            {
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, SGame.ConstructorHack.Monitor, SGame.ConstructorHack.Reflection, SGame.ConstructorHack.JsonHelper, this.OnLoadingFirstAsset ?? SGame.ConstructorHack?.OnLoadingFirstAsset);
                this.NextContentManagerIsMain = true;
                return this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
            }

            // Game1.content initializing from LoadContent
            if (this.NextContentManagerIsMain)
            {
                this.NextContentManagerIsMain = false;
                SGameConsole.Instance.InitializeContent(this.ContentCore.MainContentManager);
                return this.ContentCore.MainContentManager;
            }

            // any other content manager
            return this.ContentCore.CreateGameContentManager("(generated)");
        }

        /// <summary>The method called when the game is updating its state. This happens roughly 60 times per second.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        protected override void Update(GameTime gameTime)
        {
            if (this.IsGameSuspended)
            {
                if (!this.IsAfterInitialize)
                    this.IsAfterInitialize = true;

                return;
            }

            var events = this.Events;

            try
            {
                this.DeprecationManager.PrintQueued();
                this.PerformanceMonitor.PrintQueuedAlerts();

                /*********
                ** First-tick initialization
                *********/
                if (!this.IsInitialized)
                {
                    this.IsInitialized = true;
                    this.InitializeAfterGameStarted();
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
                    //this.Monitor.Log("Game loader synchronizing...", LogLevel.Trace);
                    while (Game1.currentLoader?.MoveNext() == true)
                    {
                        // raise load stage changed
                        switch (Game1.currentLoader.Current)
                        {
                            case 1:
                            case 24:
                                return;

                            case 20:
                                if (!saveParsed && SaveGame.loaded != null)
                                {
                                    this.Monitor.Log("SaveParsed", LogLevel.Debug);
                                    saveParsed = true;
                                    this.OnLoadStageChanged(LoadStage.SaveParsed);
                                }
                                return;

                            case 36:
                                this.Monitor.Log("SaveLoadedBasicInfo", LogLevel.Debug);
                                this.OnLoadStageChanged(LoadStage.SaveLoadedBasicInfo);
                                break;

                            case 50:
                                this.Monitor.Log("SaveLoadedLocations", LogLevel.Debug);
                                this.OnLoadStageChanged(LoadStage.SaveLoadedLocations);
                                break;

                            default:
                                if (Game1.gameMode == Game1.playingGameMode)
                                {
                                    this.Monitor.Log("Preloaded", LogLevel.Debug);
                                    this.OnLoadStageChanged(LoadStage.Preloaded);
                                }   
                                break;
                        }
                    }

                    Game1.currentLoader = null;
                    this.Monitor.Log("Game loader done.", LogLevel.Trace);
                }
                if (Game1._newDayTask?.Status == TaskStatus.Created)
                {
                    this.Monitor.Log("New day task synchronizing...", LogLevel.Trace);
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
                // update tick are negligible and not worth the complications of bypassing Game1.Update.
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
                ** Reload assets when interceptors are added/removed
                *********/
                if (this.ReloadAssetInterceptorsQueue.Any())
                {
                    // get unique interceptors
                    AssetInterceptorChange[] interceptors = this.ReloadAssetInterceptorsQueue
                        .GroupBy(p => p.Instance, new ObjectReferenceComparer<object>())
                        .Select(p => p.First())
                        .ToArray();
                    this.ReloadAssetInterceptorsQueue.Clear();

                    // log summary
                    this.Monitor.Log("Invalidating cached assets for new editors & loaders...");
                    this.Monitor.Log(
                        "   changed: "
                        + string.Join(", ",
                            interceptors
                                .GroupBy(p => p.Mod)
                                .OrderBy(p => p.Key.DisplayName)
                                .Select(modGroup =>
                                    $"{modGroup.Key.DisplayName} ("
                                    + string.Join(", ", modGroup.GroupBy(p => p.WasAdded).ToDictionary(p => p.Key, p => p.Count()).Select(p => $"{(p.Key ? "added" : "removed")} {p.Value}"))
                                    + ")"
                            )
                        )
                    );

                    // reload affected assets
                    this.ContentCore.InvalidateCache(asset => interceptors.Any(p => p.CanIntercept(asset)));
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
                    if (Game1.dayOfMonth != 0) // wait until new-game intro finishes (world not fully initialized yet)
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
                ** Display in-game warnings
                *********/
                // save content removed
                if (this.IsSaveContentRemoved && Context.IsWorldReady)
                {
                    this.IsSaveContentRemoved = false;
                    Game1.addHUDMessage(new HUDMessage(this.Translator.Get("warn.invalid-content-removed"), HUDMessage.error_type));
                }

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
                                SButtonState status = pair.Value;

                                if (status == SButtonState.Pressed)
                                {
                                    if (this.Monitor.IsVerbose)
                                        this.Monitor.Log($"Events: button {button} pressed.", LogLevel.Trace);

                                    events.ButtonPressed.Raise(new ButtonPressedEventArgs(button, cursor, inputState));
                                }
                                else if (status == SButtonState.Released)
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
                        IClickableMenu was = state.ActiveMenu.Old;
                        IClickableMenu now = state.ActiveMenu.New;

                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Context: menu changed from {state.ActiveMenu.Old?.GetType().FullName ?? "none"} to {state.ActiveMenu.New?.GetType().FullName ?? "none"}.", LogLevel.Trace);

                        // raise menu events
                        events.MenuChanged.Raise(new MenuChangedEventArgs(was, now));

                        if (now is GameMenu gameMenu)
                        {
                            foreach (IClickableMenu menu in gameMenu.pages)
                            {
                                OptionsPage optionsPage = menu as OptionsPage;
                                if (optionsPage != null)
                                {
                                    List<OptionsElement> options = this.Reflection.GetField<List<OptionsElement>>(optionsPage, "options").GetValue();
                                    options.Insert(0, new OptionsButton("Console", () => SGameConsole.Instance.Show()));
                                    this.Reflection.GetMethod(optionsPage, "updateContentPositions").Invoke();
                                }
                            }
                        }
                        else if (now is ShopMenu shopMenu)
                        {
                            Dictionary<ISalable, int[]> itemPriceAndStock = this.Reflection.GetField<Dictionary<ISalable, int[]>>(shopMenu, "itemPriceAndStock").GetValue();
                            if (shopMenu.forSaleButtons.Count < itemPriceAndStock.Keys.Select(item => item.Name).Distinct().Count())
                            {
                                this.Monitor.Log($"Shop Menu Pop");
                                Game1.activeClickableMenu = new ShopMenu(itemPriceAndStock,
                                    this.Reflection.GetField<int>(shopMenu, "currency").GetValue(),
                                    this.Reflection.GetField<string>(shopMenu, "personName").GetValue(),
                                    shopMenu.onPurchase, shopMenu.onSell, shopMenu.storeContext);
                            }
                        }
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

                                // chest items changed
                                if (events.ChestInventoryChanged.HasListeners())
                                {
                                    foreach (var pair in locState.ChestItems)
                                    {
                                        SnapshotItemListDiff diff = pair.Value;
                                        events.ChestInventoryChanged.Raise(new ChestInventoryChangedEventArgs(pair.Key, location, added: diff.Added, removed: diff.Removed, quantityChanged: diff.QuantityChanged));
                                    }
                                }

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
                            if (playerState.Inventory.IsChanged)
                            {
                                var inventory = playerState.Inventory;

                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log("Events: player inventory changed.", LogLevel.Trace);
                                events.InventoryChanged.Raise(new InventoryChangedEventArgs(player, added: inventory.Added, removed: inventory.Removed, quantityChanged: inventory.QuantityChanged));
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
        /// <param name="target_screen">The render target, if any.</param>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "copied from game code as-is")]
        protected override void _draw(GameTime gameTime, RenderTarget2D target_screen, RenderTarget2D toBuffer = null)
        {
            Context.IsInDrawLoop = true;
            try
            {
                if (SGameConsole.Instance.isVisible)
                {
                    Game1.game1.GraphicsDevice.SetRenderTarget(Game1.game1.screen);
                    Game1.game1.GraphicsDevice.Clear(Color.Black);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.PointClamp,
                        null,
                        null,
                        null,
                        null);
                    SGameConsole.Instance.draw(Game1.spriteBatch);
                    Game1.spriteBatch.End();
                    Game1.game1.GraphicsDevice.SetRenderTarget(null);
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.LinearClamp,
                        DepthStencilState.Default,
                        RasterizerState.CullNone,
                        null,
                        null);
                    Game1.spriteBatch.Draw(Game1.game1.screen,
                        Vector2.Zero,
                        new Microsoft.Xna.Framework.Rectangle?(Game1.game1.screen.Bounds),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        Game1.options.zoomLevel,
                        SpriteEffects.None,
                        1f);
                    Game1.spriteBatch.End();
                    return;
                }
                this.DrawImpl(gameTime, target_screen, toBuffer);
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
        /// <param name="target_screen">The render target, if any.</param>
        /// <remarks>This implementation is identical to <see cref="Game1.Draw"/>, except for try..catch around menu draw code, private field references replaced by wrappers, and added events.</remarks>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "LocalVariableHidesMember", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "PossibleLossOfFraction", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantArgumentDefaultValue", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantCast", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantExplicitNullableCreation", Justification = "copied from game code as-is")]
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod", Justification = "copied from game code as-is")]
        [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField", Justification = "copied from game code as-is")]
        [SuppressMessage("SMAPI.CommonErrors", "AvoidImplicitNetFieldCast", Justification = "copied from game code as-is")]
        private void DrawImpl(GameTime gameTime, RenderTarget2D target_screen, RenderTarget2D toBuffer = null)
        {
            var events = this.Events;
            if (skipNextDrawCall)
            {
                skipNextDrawCall = false;
            }
            else
            {
                IReflectedField<bool> _drawActiveClickableMenu = this.Reflection.GetField<bool>(this, "_drawActiveClickableMenu");
                IReflectedField<string> _spriteBatchBeginNextID = this.Reflection.GetField<string>(typeof(Game1), "_spriteBatchBeginNextID");
                IReflectedField<bool> _drawHUD = this.Reflection.GetField<bool>(this, "_drawHUD");
                IReflectedField<List<Farmer>> _farmerShadows = this.Reflection.GetField<List<Farmer>>(this, "_farmerShadows");
                IReflectedField<StringBuilder> _debugStringBuilder = this.Reflection.GetField<StringBuilder>(typeof(Game1), "_debugStringBuilder");
                IReflectedField<BlendState> lightingBlend = this.Reflection.GetField<BlendState>(this, "lightingBlend");

                IReflectedMethod SpriteBatchBegin = this.Reflection.GetMethod(this, "SpriteBatchBegin", new Type[] { typeof(float) });
                IReflectedMethod _spriteBatchBegin = this.Reflection.GetMethod(this, "_spriteBatchBegin", new Type[] { typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState), typeof(Effect), typeof(Matrix) });
                IReflectedMethod _spriteBatchEnd = this.Reflection.GetMethod(this, "_spriteBatchEnd", new Type[] { });
                IReflectedMethod DrawLoadingDotDotDot = this.Reflection.GetMethod(this, "DrawLoadingDotDotDot", new Type[] { typeof(GameTime) });
                IReflectedMethod CheckToReloadGameLocationAfterDrawFail = this.Reflection.GetMethod(this, "CheckToReloadGameLocationAfterDrawFail", new Type[] { typeof(string), typeof(Exception) });
                IReflectedMethod DrawTapToMoveTarget = this.Reflection.GetMethod(this, "DrawTapToMoveTarget", new Type[] { });
                IReflectedMethod DrawDayTimeMoneyBox = this.Reflection.GetMethod(this, "DrawDayTimeMoneyBox", new Type[] { });
                IReflectedMethod DrawAfterMap = this.Reflection.GetMethod(this, "DrawAfterMap", new Type[] { });
                IReflectedMethod DrawToolbar = this.Reflection.GetMethod(this, "DrawToolbar", new Type[] { });
                IReflectedMethod DrawVirtualJoypad = this.Reflection.GetMethod(this, "DrawVirtualJoypad", new Type[] { });
                IReflectedMethod DrawMenuMouseCursor = this.Reflection.GetMethod(this, "DrawMenuMouseCursor", new Type[] { });
                IReflectedMethod DrawFadeToBlackFullScreenRect = this.Reflection.GetMethod(this, "DrawFadeToBlackFullScreenRect", new Type[] { });
                IReflectedMethod DrawChatBox = this.Reflection.GetMethod(this, "DrawChatBox", new Type[] { });
                IReflectedMethod DrawDialogueBoxForPinchZoom = this.Reflection.GetMethod(this, "DrawDialogueBoxForPinchZoom", new Type[] { });
                IReflectedMethod DrawUnscaledActiveClickableMenuForPinchZoom = this.Reflection.GetMethod(this, "DrawUnscaledActiveClickableMenuForPinchZoom", new Type[] { });
                IReflectedMethod DrawNativeScaledActiveClickableMenuForPinchZoom = this.Reflection.GetMethod(this, "DrawNativeScaledActiveClickableMenuForPinchZoom", new Type[] { });
                IReflectedMethod DrawHUDMessages = this.Reflection.GetMethod(this, "DrawHUDMessages", new Type[] { });
                IReflectedMethod DrawTutorialUI = this.Reflection.GetMethod(this, "DrawTutorialUI", new Type[] { });
                IReflectedMethod DrawGreenPlacementBounds = this.Reflection.GetMethod(this, "DrawGreenPlacementBounds", new Type[] { });

                _drawHUD.SetValue(false);
                _drawActiveClickableMenu.SetValue(false);
                Game1.showingHealthBar = false;
                if (_newDayTask != null)
                {
                    base.GraphicsDevice.Clear(Game1.bgColor);
                    if (!Game1.showInterDayScroll)
                        return;
                    this.DrawSavingDotDotDot();
                }
                else
                {
                    if (target_screen != null && toBuffer == null)
                    {
                        this.GraphicsDevice.SetRenderTarget(target_screen);
                    }
                    if (this.IsSaving)
                    {
                        base.GraphicsDevice.Clear(Game1.bgColor);
                        this.renderScreenBuffer(BlendState.Opaque, toBuffer);
                        if (activeClickableMenu != null)
                        {
                            if (IsActiveClickableMenuNativeScaled)
                            {
                                BackupViewportAndZoom(divideByZoom: true);
                                SetSpriteBatchBeginNextID("A1");
                                SpriteBatchBegin.Invoke(NativeZoomLevel);
                                events.Rendering.RaiseEmpty();
                                try
                                {
                                    events.RenderingActiveMenu.RaiseEmpty();
                                    Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                    events.RenderedActiveMenu.RaiseEmpty();
                                }
                                catch (Exception ex)
                                {
                                    this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during save. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                    Game1.activeClickableMenu.exitThisMenu();
                                }
                                _spriteBatchEnd.Invoke();
                                RestoreViewportAndZoom();
                            }
                            else
                            {
                                BackupViewportAndZoom();
                                SetSpriteBatchBeginNextID("A2");
                                SpriteBatchBegin.Invoke(1f);
                                events.Rendering.RaiseEmpty();
                                try
                                {
                                    events.RenderingActiveMenu.RaiseEmpty();
                                    Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                    events.RenderedActiveMenu.RaiseEmpty();
                                }
                                catch (Exception ex)
                                {
                                    this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during save. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                    Game1.activeClickableMenu.exitThisMenu();
                                }
                                events.Rendered.RaiseEmpty();

                                _spriteBatchEnd.Invoke();
                                RestoreViewportAndZoom();
                            }
                        }
                        if (overlayMenu == null)
                            return;
                        BackupViewportAndZoom(false);
                        SetSpriteBatchBeginNextID("B");
                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        overlayMenu.draw(spriteBatch);
                        _spriteBatchEnd.Invoke();
                        RestoreViewportAndZoom();
                    }
                    else
                    {
                        base.GraphicsDevice.Clear(Game1.bgColor);
                        if (activeClickableMenu != null && options.showMenuBackground && (activeClickableMenu.showWithoutTransparencyIfOptionIsSet() && !this.takingMapScreenshot))
                        {
                            Matrix scale = Matrix.CreateScale(1f);
                            SetSpriteBatchBeginNextID("C");
                            _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, scale);
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

                            _spriteBatchEnd.Invoke();
                            this.drawOverlays(spriteBatch, true);
                            this.renderScreenBufferTargetScreen(target_screen);
                            if (overlayMenu == null)
                                return;
                            SetSpriteBatchBeginNextID("D");
                            _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                            overlayMenu.draw(spriteBatch);
                            _spriteBatchEnd.Invoke();
                        }
                        else
                        {
                            if (emergencyLoading)
                            {
                                if (!SeenConcernedApeLogo)
                                {
                                    SetSpriteBatchBeginNextID("E");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (logoFadeTimer < 5000)
                                    {
                                        spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.White);
                                    }
                                    if (logoFadeTimer > 4500)
                                    {
                                        float scale = System.Math.Min(1f, (float)(logoFadeTimer - 4500) / 500f);
                                        spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * scale);
                                    }
                                    spriteBatch.Draw(titleButtonsTexture, new Vector2(Game1.viewport.Width / 2, Game1.viewport.Height / 2 - 90), new Microsoft.Xna.Framework.Rectangle(171 + ((logoFadeTimer / 100 % 2 == 0) ? 111 : 0), 311, 111, 60), Color.White * ((logoFadeTimer < 500) ? ((float)logoFadeTimer / 500f) : ((logoFadeTimer > 4500) ? (1f - (float)(logoFadeTimer - 4500) / 500f) : 1f)), 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.2f);
                                    spriteBatch.Draw(titleButtonsTexture, new Vector2(Game1.viewport.Width / 2 - 261, Game1.viewport.Height / 2 - 102), new Microsoft.Xna.Framework.Rectangle((logoFadeTimer / 100 % 2 == 0) ? 85 : 0, 306, 85, 69), Color.White * ((logoFadeTimer < 500) ? ((float)logoFadeTimer / 500f) : ((logoFadeTimer > 4500) ? (1f - (float)(logoFadeTimer - 4500) / 500f) : 1f)), 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.2f);
                                    _spriteBatchEnd.Invoke();
                                }
                                logoFadeTimer -= gameTime.ElapsedGameTime.Milliseconds;
                            }
                            if (gameMode == (byte)11)
                            {
                                SetSpriteBatchBeginNextID("F");
                                _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                events.Rendering.RaiseEmpty();
                                spriteBatch.DrawString(dialogueFont, content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3685"), new Vector2(16f, 16f), Color.HotPink);
                                spriteBatch.DrawString(dialogueFont, content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3686"), new Vector2(16f, 32f), new Color(0, 255, 0));
                                spriteBatch.DrawString(dialogueFont, parseText(errorMessage, dialogueFont, graphics.GraphicsDevice.Viewport.Width), new Vector2(16f, 48f), Color.White);
                                events.Rendered.RaiseEmpty();

                                _spriteBatchEnd.Invoke();
                                return;
                            }
                            else if (currentMinigame != null)
                            {
                                currentMinigame.draw(spriteBatch);
                                if (globalFade && !menuUp && (!nameSelectUp || messagePause))
                                {
                                    SetSpriteBatchBeginNextID("G");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((gameMode == 0) ? (1f - fadeToBlackAlpha) : fadeToBlackAlpha));
                                    _spriteBatchEnd.Invoke();
                                }
                                this.drawOverlays(spriteBatch, true);
                                this.renderScreenBufferTargetScreen(target_screen);
                                switch (Game1.currentMinigame)
                                {
                                    case FishingGame _ when Game1.activeClickableMenu != null:
                                        Game1.SetSpriteBatchBeginNextID("A-A");
                                        SpriteBatchBegin.Invoke(1f);
                                        Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                        _spriteBatchEnd.Invoke();
                                        this.drawOverlays(Game1.spriteBatch, true);
                                        break;
                                    case FantasyBoardGame _ when Game1.activeClickableMenu != null:
                                        if (Game1.IsActiveClickableMenuNativeScaled)
                                        {
                                            Game1.BackupViewportAndZoom(true);
                                            Game1.SetSpriteBatchBeginNextID("A1");
                                            SpriteBatchBegin.Invoke(Game1.NativeZoomLevel);
                                            Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                            _spriteBatchEnd.Invoke();
                                            Game1.RestoreViewportAndZoom();
                                            break;
                                        }
                                        Game1.BackupViewportAndZoom(false);
                                        Game1.SetSpriteBatchBeginNextID("A2");
                                        SpriteBatchBegin.Invoke(1f);
                                        Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                        _spriteBatchEnd.Invoke();
                                        Game1.RestoreViewportAndZoom();
                                        break;
                                }
                                DrawVirtualJoypad.Invoke();
                            }
                            else if (showingEndOfNightStuff)
                            {
                                this.renderScreenBuffer(BlendState.Opaque, null);
                                BackupViewportAndZoom(divideByZoom: true);
                                SetSpriteBatchBeginNextID("A-B");
                                SpriteBatchBegin.Invoke(NativeZoomLevel);
                                events.Rendering.RaiseEmpty();
                                if (activeClickableMenu != null)
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

                                _spriteBatchEnd.Invoke();
                                this.drawOverlays(spriteBatch, true);
                                RestoreViewportAndZoom();
                            }
                            else if (gameMode == (byte)6 || gameMode == (byte)3 && currentLocation == null)
                            {
                                SpriteBatchBegin.Invoke(1f);
                                events.Rendering.RaiseEmpty();
                                _spriteBatchEnd.Invoke();
                                DrawLoadingDotDotDot.Invoke(gameTime);
                                SpriteBatchBegin.Invoke(1f);
                                events.Rendered.RaiseEmpty();
                                _spriteBatchEnd.Invoke();

                                this.drawOverlays(spriteBatch);
                                this.renderScreenBufferTargetScreen(target_screen);
                                if (overlayMenu != null)
                                {
                                    SetSpriteBatchBeginNextID("H");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    overlayMenu.draw(spriteBatch);
                                    _spriteBatchEnd.Invoke();
                                }
                                //base.Draw(gameTime);
                            }
                            else
                            {
                                Microsoft.Xna.Framework.Rectangle rectangle;
                                Viewport viewport;
                                byte batchOpens = 0;
                                if (gameMode == (byte) 0)
                                {
                                    SetSpriteBatchBeginNextID("I");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if(++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                }
                                else if (!drawGame)
                                {
                                    SetSpriteBatchBeginNextID("J");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                }
                                else if (drawGame)
                                {
                                    if (drawLighting)
                                    {
                                        base.GraphicsDevice.SetRenderTarget(lightmap);
                                        base.GraphicsDevice.Clear(Color.White * 0f);
                                        SetSpriteBatchBeginNextID("K");
                                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, new Matrix?());
                                        if (++batchOpens == 1)
                                            events.Rendering.RaiseEmpty();
                                        spriteBatch.Draw(staminaRect, lightmap.Bounds, currentLocation.Name.StartsWith("UndergroundMine") ? mine.getLightingColor(gameTime) : (ambientLight.Equals(Color.White) || RainManager.Instance.isRaining && (bool)((NetFieldBase<bool, NetBool>)Game1.currentLocation.isOutdoors) ? Game1.outdoorLight : Game1.ambientLight));
                                        foreach (LightSource currentLightSource in currentLightSources)
                                        {
                                            if (!RainManager.Instance.isRaining && !Game1.isDarkOut() || currentLightSource.lightContext.Value != LightSource.LightContext.WindowLight)
                                            {
                                                if (currentLightSource.PlayerID != 0L && currentLightSource.PlayerID != Game1.player.UniqueMultiplayerID)
                                                {
                                                    Farmer farmerMaybeOffline = Game1.getFarmerMaybeOffline(currentLightSource.PlayerID);
                                                    if (farmerMaybeOffline == null || farmerMaybeOffline.currentLocation != null && farmerMaybeOffline.currentLocation.Name != Game1.currentLocation.Name || (bool) ((NetFieldBase<bool, NetBool>) farmerMaybeOffline.hidden))
                                                        continue;
                                                }
                                            }
                                            if (Utility.isOnScreen((Vector2)((NetFieldBase<Vector2, NetVector2>)currentLightSource.position), (int)((double)(float)((NetFieldBase<float, NetFloat>)currentLightSource.radius) * 64.0 * 4.0)))
                                            {
                                                SpriteBatch spriteBatch = Game1.spriteBatch;
                                                Texture2D lightTexture = currentLightSource.lightTexture;
                                                Vector2 position = Game1.GlobalToLocal(Game1.viewport, (Vector2)((NetFieldBase<Vector2, NetVector2>)currentLightSource.position)) / (float)(Game1.options.lightingQuality / 2);
                                                Microsoft.Xna.Framework.Rectangle? sourceRectangle = new Microsoft.Xna.Framework.Rectangle?(currentLightSource.lightTexture.Bounds);
                                                Color color = (Color)((NetFieldBase<Color, NetColor>)currentLightSource.color);
                                                Microsoft.Xna.Framework.Rectangle bounds = currentLightSource.lightTexture.Bounds;
                                                double x = (double)bounds.Center.X;
                                                bounds = currentLightSource.lightTexture.Bounds;
                                                double y = (double)bounds.Center.Y;
                                                Vector2 origin = new Vector2((float)x, (float)y);
                                                double num = (double)(float)((NetFieldBase<float, NetFloat>)currentLightSource.radius) / (double)(Game1.options.lightingQuality / 2);

                                                spriteBatch.Draw(lightTexture, position, sourceRectangle, color, 0.0f, origin, (float) num, SpriteEffects.None, 0.9f);
                                            }
                                        }
                                        _spriteBatchEnd.Invoke();
                                        base.GraphicsDevice.SetRenderTarget(target_screen);
                                    }
                                    if (bloomDay && bloom != null)
                                    {
                                        bloom.BeginDraw();
                                    }
                                    base.GraphicsDevice.Clear(Game1.bgColor);
                                    SetSpriteBatchBeginNextID("L");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                    events.RenderingWorld.RaiseEmpty();
                                    _spriteBatchBeginNextID.SetValue("L1");
                                    if (background != null)
                                    {
                                        background.draw(spriteBatch);
                                    }
                                    _spriteBatchBeginNextID.SetValue("L2");
                                    mapDisplayDevice.BeginScene(spriteBatch);
                                    _spriteBatchBeginNextID.SetValue("L3");
                                    try
                                    {
                                        if (Game1.currentLocation != null)
                                        {
                                            currentLocation.Map.GetLayer("Back").Draw(mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                                            _spriteBatchBeginNextID.SetValue("L4");
                                        }
                                    }
                                    catch (KeyNotFoundException exception)
                                    {
                                        CheckToReloadGameLocationAfterDrawFail.Invoke("Back", exception);
                                    }
                                    _spriteBatchBeginNextID.SetValue("L5");
                                    if (Game1.currentLocation != null)
                                    {
                                        currentLocation.drawWater(spriteBatch);
                                    }
                                    _spriteBatchBeginNextID.SetValue("L6");
                                    _farmerShadows.GetValue().Clear();
                                    _spriteBatchBeginNextID.SetValue("L7");
                                    if (currentLocation.currentEvent != null && !currentLocation.currentEvent.isFestival && currentLocation.currentEvent.farmerActors.Count > 0)
                                    {
                                        _spriteBatchBeginNextID.SetValue("L8");
                                        foreach (Farmer farmerActor in currentLocation.currentEvent.farmerActors)
                                        {
                                            if ((farmerActor.IsLocalPlayer && displayFarmer) || !farmerActor.hidden)
                                            {
                                                _farmerShadows.GetValue().Add(farmerActor);
                                            }
                                        }
                                        _spriteBatchBeginNextID.SetValue("L9");
                                    }
                                    else
                                    {
                                        _spriteBatchBeginNextID.SetValue("L10");
                                        if (currentLocation != null)
                                        {
                                            _spriteBatchBeginNextID.SetValue("L11");
                                            foreach (Farmer farmer in currentLocation.farmers)
                                            {
                                                if ((farmer.IsLocalPlayer && displayFarmer) || !farmer.hidden)
                                                {
                                                    _farmerShadows.GetValue().Add(farmer);
                                                }
                                            }
                                            _spriteBatchBeginNextID.SetValue("L12");
                                        }             
                                    }
                                    _spriteBatchBeginNextID.SetValue("L13");
                                    if (currentLocation != null && !currentLocation.shouldHideCharacters())
                                    {
                                        _spriteBatchBeginNextID.SetValue("L14");
                                        if (CurrentEvent == null)
                                        {
                                            _spriteBatchBeginNextID.SetValue("L15");
                                            foreach (NPC character in currentLocation.characters)
                                            {
                                                try
                                                {
                                                    if (!character.swimming && !character.HideShadow && !character.IsInvisible && !currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(character.getTileLocation()))
                                                    {
                                                        spriteBatch.Draw(shadowTexture, GlobalToLocal(Game1.viewport, character.Position + new Vector2((float)(character.Sprite.SpriteWidth * 4) / 2f, character.GetBoundingBox().Height + ((!character.IsMonster) ? 12 : 0))), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), (4f + (float)character.yJumpOffset / 40f) * (float)character.scale, SpriteEffects.None, System.Math.Max(0f, (float)character.getStandingY() / 10000f) - 1E-06f);
                                                    }
                                                }
                                                catch (System.Exception ex)
                                                {
                                                    Dictionary<string, string> dictionary1 = new Dictionary<string, string>();
                                                    if (character != null)
                                                    {
                                                        dictionary1["name"] = (string)(NetFieldBase<string, NetString>)character.name;
                                                        dictionary1["Sprite"] = (character.Sprite != null).ToString();
                                                        Dictionary<string, string> dictionary2 = dictionary1;
                                                        character.GetBoundingBox();
                                                        bool flag = true;
                                                        string str1 = flag.ToString();
                                                        dictionary2["BoundingBox"] = str1;
                                                        Dictionary<string, string> dictionary3 = dictionary1;
                                                        flag = true;
                                                        string str2 = flag.ToString();
                                                        dictionary3["shadowTexture.Bounds"] = str2;
                                                        Dictionary<string, string> dictionary4 = dictionary1;
                                                        flag = Game1.currentLocation != null;
                                                        string str3 = flag.ToString();
                                                        dictionary4["currentLocation"] = str3;
                                                    }
                                                    Dictionary<string, string> dictionary5 = dictionary1;
                                                    // Ignored
                                                    //ErrorAttachmentLog[] errorAttachmentLogArray = Array.Empty<ErrorAttachmentLog>();
                                                    //Microsoft.AppCenter.Crashes.Crashes.TrackError(ex, (IDictionary<string, string>)dictionary5, errorAttachmentLogArray);
                                                }
                                            }
                                            _spriteBatchBeginNextID.SetValue("L16");
                                        }
                                        else
                                        {
                                            _spriteBatchBeginNextID.SetValue("L17");
                                            foreach (NPC actor in CurrentEvent.actors)
                                            {
                                                if (!actor.swimming && !actor.HideShadow && !currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(actor.getTileLocation()))
                                                {
                                                    spriteBatch.Draw(shadowTexture, GlobalToLocal(Game1.viewport, actor.Position + new Vector2((float)(actor.Sprite.SpriteWidth * 4) / 2f, actor.GetBoundingBox().Height + ((!actor.IsMonster) ? ((actor.Sprite.SpriteHeight <= 16) ? (-4) : 12) : 0))), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), (4f + (float)actor.yJumpOffset / 40f) * (float)actor.scale, SpriteEffects.None, System.Math.Max(0f, (float)actor.getStandingY() / 10000f) - 1E-06f);
                                                }
                                            }
                                            _spriteBatchBeginNextID.SetValue("L18");
                                        }
                                        _spriteBatchBeginNextID.SetValue("L19");
                                        foreach (Farmer farmerShadow in _farmerShadows.GetValue())
                                        {
                                            if (!Game1.multiplayer.isDisconnecting(farmerShadow.UniqueMultiplayerID) && !farmerShadow.swimming && !farmerShadow.isRidingHorse() && (currentLocation == null || !currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmerShadow.getTileLocation())))
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
                                                double num = 4.0 - (!farmerShadow.running && !farmerShadow.UsingTool || farmerShadow.FarmerSprite.currentAnimationIndex <= 1 ? 0.0 : (double)System.Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmerShadow.FarmerSprite.CurrentFrame]) * 0.5);
                                                spriteBatch.Draw(shadowTexture, local, sourceRectangle, white, 0.0f, origin, (float)num, SpriteEffects.None, 0.0f);
                                            }
                                        }
                                        _spriteBatchBeginNextID.SetValue("L20");
                                    }
                                    _spriteBatchBeginNextID.SetValue("L21");
                                    try
                                    {
                                        if (currentLocation != null)
                                        {
                                            currentLocation.Map.GetLayer("Buildings").Draw(mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                                        }
                                    }
                                    catch (KeyNotFoundException exception2)
                                    {
                                        CheckToReloadGameLocationAfterDrawFail.Invoke("Buildings", exception2);
                                    }
                                    _spriteBatchBeginNextID.SetValue("L22");
                                    mapDisplayDevice.EndScene();
                                    _spriteBatchBeginNextID.SetValue("L23");
                                    if (currentLocation != null && currentLocation.tapToMove.targetNPC != null)
                                    {
                                        spriteBatch.Draw(mouseCursors, GlobalToLocal(Game1.viewport, currentLocation.tapToMove.targetNPC.Position + new Vector2((float)(currentLocation.tapToMove.targetNPC.Sprite.SpriteWidth * 4) / 2f - 32f, currentLocation.tapToMove.targetNPC.GetBoundingBox().Height + ((!currentLocation.tapToMove.targetNPC.IsMonster) ? 12 : 0) - 32)), new Microsoft.Xna.Framework.Rectangle(194, 388, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.58f);
                                    }
                                    _spriteBatchBeginNextID.SetValue("L24");
                                    _spriteBatchEnd.Invoke();
                                    _spriteBatchBeginNextID.SetValue("L25");
                                    SetSpriteBatchBeginNextID("M");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    _spriteBatchBeginNextID.SetValue("M1");
                                    if (!currentLocation.shouldHideCharacters())
                                    {
                                        _spriteBatchBeginNextID.SetValue("M2");
                                        if (CurrentEvent == null)
                                        {
                                            _spriteBatchBeginNextID.SetValue("M3");
                                            foreach (NPC character2 in currentLocation.characters)
                                            {
                                                if (!character2.swimming && !character2.HideShadow && currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(character2.getTileLocation()))
                                                {
                                                    spriteBatch.Draw(shadowTexture, GlobalToLocal(Game1.viewport, character2.Position + new Vector2((float)(character2.Sprite.SpriteWidth * 4) / 2f, character2.GetBoundingBox().Height + ((!character2.IsMonster) ? 12 : 0))), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), (4f + (float)character2.yJumpOffset / 40f) * (float)character2.scale, SpriteEffects.None, System.Math.Max(0f, (float)character2.getStandingY() / 10000f) - 1E-06f);
                                                }
                                            }
                                            _spriteBatchBeginNextID.SetValue("M4");
                                        }
                                        else
                                        {
                                            _spriteBatchBeginNextID.SetValue("M5");
                                            foreach (NPC actor2 in CurrentEvent.actors)
                                            {
                                                if (!actor2.swimming && !actor2.HideShadow && currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(actor2.getTileLocation()))
                                                {
                                                    spriteBatch.Draw(shadowTexture, GlobalToLocal(Game1.viewport, actor2.Position + new Vector2((float)(actor2.Sprite.SpriteWidth * 4) / 2f, actor2.GetBoundingBox().Height + ((!actor2.IsMonster) ? 12 : 0))), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), (4f + (float)actor2.yJumpOffset / 40f) * (float)actor2.scale, SpriteEffects.None, System.Math.Max(0f, (float)actor2.getStandingY() / 10000f) - 1E-06f);
                                                }
                                            }
                                            _spriteBatchBeginNextID.SetValue("M6");
                                        }
                                        foreach (Farmer farmerShadow in _farmerShadows.GetValue())
                                        {
                                            _spriteBatchBeginNextID.SetValue("M7");
                                            if (!farmerShadow.swimming && !farmerShadow.isRidingHorse() && currentLocation != null && currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmerShadow.getTileLocation()))
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
                                                double num = 4.0 - (!farmerShadow.running && !farmerShadow.UsingTool || farmerShadow.FarmerSprite.currentAnimationIndex <= 1 ? 0.0 : (double)System.Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmerShadow.FarmerSprite.CurrentFrame]) * 0.5);
                                                spriteBatch.Draw(shadowTexture, local, sourceRectangle, white, 0.0f, origin, (float)num, SpriteEffects.None, 0.0f);
                                            }
                                            _spriteBatchBeginNextID.SetValue("M8");
                                        }
                                    }
                                    _spriteBatchBeginNextID.SetValue("M9");
                                    if ((eventUp || killScreen) && !killScreen && currentLocation.currentEvent != null)
                                    {
                                        _spriteBatchBeginNextID.SetValue("M10");
                                        currentLocation.currentEvent.draw(spriteBatch);
                                        _spriteBatchBeginNextID.SetValue("M11");
                                    }
                                    _spriteBatchBeginNextID.SetValue("M12");
                                    if (player.currentUpgrade != null && player.currentUpgrade.daysLeftTillUpgradeDone <= 3 && currentLocation.Name.Equals("Farm"))
                                    {
                                        _spriteBatchBeginNextID.SetValue("M13");
                                        spriteBatch.Draw(player.currentUpgrade.workerTexture, GlobalToLocal(Game1.viewport, player.currentUpgrade.positionOfCarpenter), player.currentUpgrade.getSourceRectangle(), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, (player.currentUpgrade.positionOfCarpenter.Y + 48f) / 10000f);
                                        _spriteBatchBeginNextID.SetValue("M14");
                                    }
                                    _spriteBatchBeginNextID.SetValue("M15");
                                    currentLocation.draw(spriteBatch);
                                    using (Dictionary<Vector2, int>.KeyCollection.Enumerator enumerator = Game1.crabPotOverlayTiles.Keys.GetEnumerator())
                                    {
                                        while (enumerator.MoveNext())
                                        {
                                            Vector2 current = enumerator.Current;
                                            Tile tile = Game1.currentLocation.Map.GetLayer("Buildings").Tiles[(int)current.X, (int)current.Y];
                                            if (tile != null)
                                            {
                                                Vector2 local = Game1.GlobalToLocal(Game1.viewport, current * 64f);
                                                Location location = new Location((int)local.X, (int)local.Y);
                                                Game1.mapDisplayDevice.DrawTile(tile, location, (float)((current.Y * 64.0 - 1.0) / 10000.0));
                                            }
                                        }
                                    }
                                    _spriteBatchBeginNextID.SetValue("M16");
                                    if (player.ActiveObject == null && (player.UsingTool || pickingTool) && player.CurrentTool != null && (!player.CurrentTool.Name.Equals("Seeds") || pickingTool))
                                    {
                                        _spriteBatchBeginNextID.SetValue("M17");
                                        drawTool(player);
                                        _spriteBatchBeginNextID.SetValue("M18");
                                    }
                                    _spriteBatchBeginNextID.SetValue("M19");
                                    if (currentLocation.Name.Equals("Farm"))
                                    {
                                        _spriteBatchBeginNextID.SetValue("M20");
                                        this.drawFarmBuildings();
                                        _spriteBatchBeginNextID.SetValue("M21");
                                    }
                                    if (tvStation >= 0)
                                    {
                                        _spriteBatchBeginNextID.SetValue("M22");
                                        spriteBatch.Draw(tvStationTexture, GlobalToLocal(Game1.viewport, new Vector2(400f, 160f)), new Microsoft.Xna.Framework.Rectangle(tvStation * 24, 0, 24, 15), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-08f);
                                        _spriteBatchBeginNextID.SetValue("M23");
                                    }
                                    _spriteBatchBeginNextID.SetValue("M24");
                                    if (panMode)
                                    {
                                        _spriteBatchBeginNextID.SetValue("M25");
                                        spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((int)System.Math.Floor((double)(getOldMouseX() + Game1.viewport.X) / 64.0) * 64 - Game1.viewport.X, (int)System.Math.Floor((double)(getOldMouseY() + Game1.viewport.Y) / 64.0) * 64 - Game1.viewport.Y, 64, 64), Color.Lime * 0.75f);
                                        _spriteBatchBeginNextID.SetValue("M26");
                                        foreach (Warp warp in currentLocation.warps)
                                        {
                                            spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(warp.X * 64 - Game1.viewport.X, warp.Y * 64 - Game1.viewport.Y, 64, 64), Color.Red * 0.75f);
                                        }
                                        _spriteBatchBeginNextID.SetValue("M27");
                                    }
                                    _spriteBatchBeginNextID.SetValue("M28");
                                    mapDisplayDevice.BeginScene(spriteBatch);
                                    _spriteBatchBeginNextID.SetValue("M29");
                                    try
                                    {
                                        currentLocation.Map.GetLayer("Front").Draw(mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                                        _spriteBatchBeginNextID.SetValue("M30");
                                    }
                                    catch (KeyNotFoundException exception3)
                                    {
                                        CheckToReloadGameLocationAfterDrawFail.Invoke("Front", exception3);
                                    }
                                    _spriteBatchBeginNextID.SetValue("M31");
                                    mapDisplayDevice.EndScene();
                                    _spriteBatchBeginNextID.SetValue("M32");
                                    currentLocation.drawAboveFrontLayer(spriteBatch);
                                    _spriteBatchBeginNextID.SetValue("M33");
                                    if (currentLocation.tapToMove.targetNPC == null && (displayHUD || eventUp) && currentBillboard == 0 && gameMode == 3 && !freezeControls && !panMode && !HostPaused)
                                    {
                                        _spriteBatchBeginNextID.SetValue("M34");
                                        DrawTapToMoveTarget.Invoke();
                                        _spriteBatchBeginNextID.SetValue("M35");
                                    }
                                    _spriteBatchBeginNextID.SetValue("M36");
                                    _spriteBatchEnd.Invoke();
                                    SetSpriteBatchBeginNextID("N");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (displayFarmer && player.ActiveObject != null && (bool)player.ActiveObject.bigCraftable && this.checkBigCraftableBoundariesForFrontLayer() && currentLocation.Map.GetLayer("Front").PickTile(new Location(player.getStandingX(), player.getStandingY()), Game1.viewport.Size) == null)
                                    {
                                        drawPlayerHeldObject(player);
                                    }
                                    else if (displayFarmer && player.ActiveObject != null)
                                    {
                                        if (Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int)Game1.player.Position.X, (int)Game1.player.Position.Y - 38), Game1.viewport.Size) == null || ((IDictionary<string, PropertyValue>)Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int)Game1.player.Position.X, (int)Game1.player.Position.Y - 38), Game1.viewport.Size).TileIndexProperties).ContainsKey("FrontAlways"))
                                        {
                                            Layer layer1 = Game1.currentLocation.Map.GetLayer("Front");
                                            rectangle = Game1.player.GetBoundingBox();
                                            Location mapDisplayLocation1 = new Location(rectangle.Right, (int)Game1.player.Position.Y - 38);
                                            Size size1 = Game1.viewport.Size;
                                            if (layer1.PickTile(mapDisplayLocation1, size1) != null)
                                            {
                                                Layer layer2 = Game1.currentLocation.Map.GetLayer("Front");
                                                rectangle = Game1.player.GetBoundingBox();
                                                Location mapDisplayLocation2 = new Location(rectangle.Right, (int)Game1.player.Position.Y - 38);
                                                Size size2 = Game1.viewport.Size;
                                                if (((IDictionary<string, PropertyValue>)layer2.PickTile(mapDisplayLocation2, size2).TileIndexProperties).ContainsKey("FrontAlways"))
                                                    goto label_168;
                                            }
                                            else
                                                goto label_168;
                                        }
                                        Game1.drawPlayerHeldObject(Game1.player);
                                    }
label_168:
                                    if ((Game1.player.UsingTool || Game1.pickingTool) && Game1.player.CurrentTool != null && ((!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool) && (Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), (int)Game1.player.Position.Y - 38), Game1.viewport.Size) != null && Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null)))
                                        Game1.drawTool(Game1.player);
                                    if (currentLocation.Map.GetLayer("AlwaysFront") != null)
                                    {
                                        mapDisplayDevice.BeginScene(spriteBatch);
                                        try
                                        {
                                            currentLocation.Map.GetLayer("AlwaysFront").Draw(mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
                                        }
                                        catch (KeyNotFoundException exception4)
                                        {
                                            CheckToReloadGameLocationAfterDrawFail.Invoke("AlwaysFront", exception4);
                                        }
                                        mapDisplayDevice.EndScene();
                                    }
                                    if (toolHold > 400f && player.CurrentTool.UpgradeLevel >= 1 && player.canReleaseTool)
                                    {
                                        Color color = Color.White;
                                        switch ((int) ((double) toolHold / 600.0) + 2)
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
                                        spriteBatch.Draw(littleEffect, new Microsoft.Xna.Framework.Rectangle((int)player.getLocalPosition(Game1.viewport).X - 2, (int)player.getLocalPosition(Game1.viewport).Y - ((!player.CurrentTool.Name.Equals("Watering Can")) ? 64 : 0) - 2, (int)(toolHold % 600f * 0.08f) + 4, 12), Color.Black);
                                        spriteBatch.Draw(littleEffect, new Microsoft.Xna.Framework.Rectangle((int)player.getLocalPosition(Game1.viewport).X, (int)player.getLocalPosition(Game1.viewport).Y - ((!player.CurrentTool.Name.Equals("Watering Can")) ? 64 : 0), (int)(toolHold % 600f * 0.08f), 8), color);
                                    }
                                    this.drawWeather(gameTime, target_screen);
                                    if (farmEvent != null)
                                    {
                                        farmEvent.draw(spriteBatch);
                                    }
                                    if (currentLocation.LightLevel > 0f && timeOfDay < 2000)
                                    {
                                        spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Black * currentLocation.LightLevel);
                                    }
                                    if (screenGlow)
                                    {
                                        spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, screenGlowColor * screenGlowAlpha);
                                    }
                                    currentLocation.drawAboveAlwaysFrontLayer(spriteBatch);
                                    if (player.CurrentTool != null && player.CurrentTool is FishingRod && ((player.CurrentTool as FishingRod).isTimingCast || (player.CurrentTool as FishingRod).castingChosenCountdown > 0f || (player.CurrentTool as FishingRod).fishCaught || (player.CurrentTool as FishingRod).showingTreasure))
                                    {
                                        player.CurrentTool.draw(spriteBatch);
                                    }
                                    _spriteBatchEnd.Invoke();
                                    SetSpriteBatchBeginNextID("O");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    if (eventUp && currentLocation.currentEvent != null)
                                    {
                                        currentLocation.currentEvent.drawAboveAlwaysFrontLayer(spriteBatch);
                                        foreach (NPC actor3 in currentLocation.currentEvent.actors)
                                        {
                                            if (actor3.isEmoting)
                                            {
                                                Vector2 localPosition = actor3.getLocalPosition(Game1.viewport);
                                                localPosition.Y -= 140f;
                                                if (actor3.Age == 2)
                                                {
                                                    localPosition.Y += 32f;
                                                }
                                                else if (actor3.Gender == 1)
                                                {
                                                    localPosition.Y += 10f;
                                                }
                                                spriteBatch.Draw(emoteSpriteSheet, localPosition, new Microsoft.Xna.Framework.Rectangle(actor3.CurrentEmoteIndex * 16 % emoteSpriteSheet.Width, actor3.CurrentEmoteIndex * 16 / emoteSpriteSheet.Width * 16, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (float)actor3.getStandingY() / 10000f);
                                            }
                                        }
                                    }
                                    _spriteBatchEnd.Invoke();
                                    if (drawLighting)
                                    {
                                        SetSpriteBatchBeginNextID("P");
                                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, lightingBlend.GetValue(), SamplerState.LinearClamp, null, null, null, new Matrix?());
                                        spriteBatch.Draw(lightmap, Vector2.Zero, lightmap.Bounds, Color.White, 0f, Vector2.Zero, options.lightingQuality / 2, SpriteEffects.None, 1f);
                                        if (RainManager.Instance.isRaining && (bool)currentLocation.isOutdoors && !(currentLocation is Desert))
                                        {
                                            spriteBatch.Draw(staminaRect, graphics.GraphicsDevice.Viewport.Bounds, Color.OrangeRed * 0.45f);
                                        }
                                        _spriteBatchEnd.Invoke();
                                    }
                                    SetSpriteBatchBeginNextID("Q");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?());
                                    events.RenderedWorld.RaiseEmpty();
                                    if (drawGrid)
                                    {
                                        int num = -Game1.viewport.X % 64;
                                        float num2 = -Game1.viewport.Y % 64;
                                        int num3 = num;
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
                                                int x = num;
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
                                    if (Game1.currentBillboard != 0 && !this.takingMapScreenshot)
                                        this.drawBillboard();
                                    if ((Game1.displayHUD || Game1.eventUp) && (Game1.currentBillboard == 0 && Game1.gameMode == (byte)3) && (!Game1.freezeControls && !Game1.panMode && !Game1.HostPaused))
                                    {
                                        if (!Game1.eventUp && Game1.farmEvent == null && (Game1.currentBillboard == 0 && Game1.gameMode == (byte)3) && (!this.takingMapScreenshot && Game1.isOutdoorMapSmallerThanViewport()))
                                        {
                                            SpriteBatch spriteBatch1 = Game1.spriteBatch;
                                            Texture2D fadeToBlackRect1 = Game1.fadeToBlackRect;
                                            int width1 = -System.Math.Min(Game1.viewport.X, 4096);
                                            viewport = Game1.graphics.GraphicsDevice.Viewport;
                                            int height1 = viewport.Height;
                                            Microsoft.Xna.Framework.Rectangle destinationRectangle1 = new Microsoft.Xna.Framework.Rectangle(0, 0, width1, height1);
                                            Color black1 = Color.Black;
                                            spriteBatch1.Draw(fadeToBlackRect1, destinationRectangle1, black1);
                                            SpriteBatch spriteBatch2 = Game1.spriteBatch;
                                            Texture2D fadeToBlackRect2 = Game1.fadeToBlackRect;
                                            int x = -Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64;
                                            viewport = Game1.graphics.GraphicsDevice.Viewport;
                                            int width2 = System.Math.Min(4096, viewport.Width - (-Game1.viewport.X + Game1.currentLocation.map.Layers[0].LayerWidth * 64));
                                            viewport = Game1.graphics.GraphicsDevice.Viewport;
                                            int height2 = viewport.Height;
                                            Microsoft.Xna.Framework.Rectangle destinationRectangle2 = new Microsoft.Xna.Framework.Rectangle(x, 0, width2, height2);
                                            Color black2 = Color.Black;
                                            spriteBatch2.Draw(fadeToBlackRect2, destinationRectangle2, black2);
                                        }
                                        _drawHUD.SetValue(false);
                                        if ((Game1.displayHUD || Game1.eventUp) && (Game1.currentBillboard == 0 && Game1.gameMode == (byte)3) && (!Game1.freezeControls && !Game1.panMode && (!Game1.HostPaused && !this.takingMapScreenshot)))
                                            _drawHUD.SetValue(true);
                                        DrawGreenPlacementBounds.Invoke();
                                    }
                                }
                                if (farmEvent != null)
                                {
                                    farmEvent.draw(spriteBatch);
                                }
                                if (dialogueUp && !nameSelectUp && !messagePause && (activeClickableMenu == null || !(activeClickableMenu is DialogueBox)))
                                {
                                    this.drawDialogueBox();
                                }
                                if (progressBar && !this.takingMapScreenshot)
                                {
                                    SpriteBatch spriteBatch1 = Game1.spriteBatch;
                                    Texture2D fadeToBlackRect = Game1.fadeToBlackRect;
                                    int x1 = (Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2;
                                    rectangle = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
                                    int y1 = rectangle.Bottom - 128;
                                    int dialogueWidth = Game1.dialogueWidth;
                                    Microsoft.Xna.Framework.Rectangle destinationRectangle1 = new Microsoft.Xna.Framework.Rectangle(x1, y1, dialogueWidth, 32);
                                    Color lightGray = Color.LightGray;
                                    spriteBatch1.Draw(fadeToBlackRect, destinationRectangle1, lightGray);
                                    SpriteBatch spriteBatch2 = Game1.spriteBatch;
                                    Texture2D staminaRect = Game1.staminaRect;
                                    int x2 = (Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2;
                                    rectangle = Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea();
                                    int y2 = rectangle.Bottom - 128;
                                    int width = (int)((double)Game1.pauseAccumulator / (double)Game1.pauseTime * (double)Game1.dialogueWidth);
                                    Microsoft.Xna.Framework.Rectangle destinationRectangle2 = new Microsoft.Xna.Framework.Rectangle(x2, y2, width, 32);
                                    Color dimGray = Color.DimGray;
                                    spriteBatch2.Draw(staminaRect, destinationRectangle2, dimGray);
                                }
                                if (RainManager.Instance.isRaining && currentLocation != null && (bool)currentLocation.isOutdoors && !(currentLocation is Desert))
                                {
                                    SpriteBatch spriteBatch = Game1.spriteBatch;
                                    Texture2D staminaRect = Game1.staminaRect;
                                    viewport = Game1.graphics.GraphicsDevice.Viewport;
                                    Microsoft.Xna.Framework.Rectangle bounds = viewport.Bounds;
                                    Color color = Color.Blue * 0.2f;
                                    spriteBatch.Draw(staminaRect, bounds, color);
                                }
                                if ((messagePause || globalFade) && (dialogueUp && !this.takingMapScreenshot))
                                {
                                    this.drawDialogueBox();
                                }
                                if (!this.takingMapScreenshot)
                                {
                                    foreach (TemporaryAnimatedSprite screenOverlayTempSprite in screenOverlayTempSprites)
                                    {
                                        screenOverlayTempSprite.draw(spriteBatch, localPosition: true, 0, 0, 1f);
                                    }
                                }
                                if (debugMode)
                                {
                                    System.Text.StringBuilder debugStringBuilder = _debugStringBuilder.GetValue();
                                    debugStringBuilder.Clear();
                                    if (panMode)
                                    {
                                        debugStringBuilder.Append((getOldMouseX() + Game1.viewport.X) / 64);
                                        debugStringBuilder.Append(",");
                                        debugStringBuilder.Append((getOldMouseY() + Game1.viewport.Y) / 64);
                                    }
                                    else
                                    {
                                        debugStringBuilder.Append("player: ");
                                        debugStringBuilder.Append(player.getStandingX() / 64);
                                        debugStringBuilder.Append(", ");
                                        debugStringBuilder.Append(player.getStandingY() / 64);
                                    }
                                    debugStringBuilder.Append(" mouseTransparency: ");
                                    debugStringBuilder.Append(mouseCursorTransparency);
                                    debugStringBuilder.Append(" mousePosition: ");
                                    debugStringBuilder.Append(getMouseX());
                                    debugStringBuilder.Append(",");
                                    debugStringBuilder.Append(getMouseY());
                                    debugStringBuilder.Append(System.Environment.NewLine);
                                    debugStringBuilder.Append(" mouseWorldPosition: ");
                                    debugStringBuilder.Append(Game1.getMouseX() + Game1.viewport.X);
                                    debugStringBuilder.Append(",");
                                    debugStringBuilder.Append(Game1.getMouseY() + Game1.viewport.Y);
                                    debugStringBuilder.Append("debugOutput: ");
                                    debugStringBuilder.Append(debugOutput);
                                    spriteBatch.DrawString(smallFont, debugStringBuilder, new Vector2(base.GraphicsDevice.Viewport.GetTitleSafeArea().X, base.GraphicsDevice.Viewport.GetTitleSafeArea().Y + smallFont.LineSpacing * 8), Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.09999999f);
                                }
                                if (showKeyHelp && !this.takingMapScreenshot)
                                {
                                    spriteBatch.DrawString(smallFont, keyHelpString, new Vector2(64f, (float)(Game1.viewport.Height - 64 - (dialogueUp ? (192 + (isQuestion ? (questionChoices.Count * 64) : 0)) : 0)) - smallFont.MeasureString(keyHelpString).Y), Color.LightGray, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
                                }
                                if (activeClickableMenu != null)
                                {
                                    _drawActiveClickableMenu.SetValue(true);
                                    events.RenderingActiveMenu.RaiseEmpty();
                                    if (activeClickableMenu is CarpenterMenu)
                                    {
                                        ((CarpenterMenu)activeClickableMenu).DrawPlacementSquares(spriteBatch);
                                    }
                                    else if (activeClickableMenu is MuseumMenu)
                                    {
                                        ((MuseumMenu)activeClickableMenu).DrawPlacementGrid(spriteBatch);
                                    }
                                    if (!IsActiveClickableMenuUnscaled && !IsActiveClickableMenuNativeScaled)
                                    {
                                        activeClickableMenu.draw(spriteBatch);
                                    }
                                    events.RenderedActiveMenu.RaiseEmpty();
                                }
                                else if (farmEvent != null)
                                {
                                    farmEvent.drawAboveEverything(spriteBatch);
                                }
                                if (Game1.emoteMenu != null && !this.takingMapScreenshot)
                                    Game1.emoteMenu.draw(Game1.spriteBatch);
                                if (HostPaused)
                                {
                                    string s = content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10378");
                                    SpriteText.drawStringWithScrollCenteredAt(spriteBatch, s, 96, 32, "", 1f, -1, 0, 0.0088f, false);
                                }
                                _spriteBatchEnd.Invoke();
                                this.drawOverlays(spriteBatch, false);
                                this.renderScreenBuffer(BlendState.Opaque, toBuffer);
                                if (_drawHUD.GetValue())
                                {
                                    DrawDayTimeMoneyBox.Invoke();
                                    SetSpriteBatchBeginNextID("A-C");
                                    SpriteBatchBegin.Invoke(1f);
                                    events.RenderingHud.RaiseEmpty();
                                    this.DrawHUD();
                                    events.RenderedHud.RaiseEmpty();
                                    if (Game1.currentLocation != null)
                                    {
                                        switch (Game1.activeClickableMenu)
                                        {
                                            case GameMenu _:
                                            case QuestLog _:
                                                break;
                                            default:
                                                Game1.currentLocation.drawAboveAlwaysFrontLayerText(spriteBatch);
                                                break;
                                        }
                                    }
                                    DrawAfterMap.Invoke();
                                    _spriteBatchEnd.Invoke();
                                    if (TutorialManager.Instance != null)
                                    {
                                        SetSpriteBatchBeginNextID("A-D");
                                        SpriteBatchBegin.Invoke(options.zoomLevel);
                                        TutorialManager.Instance.draw(spriteBatch);
                                        _spriteBatchEnd.Invoke();
                                    }
                                    DrawToolbar.Invoke();
                                    DrawMenuMouseCursor.Invoke();
                                }
                                if (_drawHUD.GetValue() || Game1.player.CanMove)
                                    DrawVirtualJoypad.Invoke();
                                DrawFadeToBlackFullScreenRect.Invoke();
                                SetSpriteBatchBeginNextID("A-E");
                                SpriteBatchBegin.Invoke(1f);
                                DrawChatBox.Invoke();
                                _spriteBatchEnd.Invoke();
                                if (_drawActiveClickableMenu.GetValue())
                                {
                                    DrawDialogueBoxForPinchZoom.Invoke();
                                    DrawUnscaledActiveClickableMenuForPinchZoom.Invoke();
                                    DrawNativeScaledActiveClickableMenuForPinchZoom.Invoke();
                                    if(IsActiveClickableMenuNativeScaled)
                                        SpriteBatchBegin.Invoke(NativeZoomLevel);
                                    else
                                        SpriteBatchBegin.Invoke(Game1.options.zoomLevel);
                                    events.Rendered.RaiseEmpty();
                                    _spriteBatchEnd.Invoke();
                                }
                                else
                                {
                                    SpriteBatchBegin.Invoke(Game1.options.zoomLevel);
                                    events.Rendered.RaiseEmpty();
                                    _spriteBatchEnd.Invoke();
                                }
                                if (_drawHUD.GetValue() && hudMessages.Count > 0 && (!eventUp || isFestival()))
                                {
                                    SetSpriteBatchBeginNextID("A-F");
                                    SpriteBatchBegin.Invoke(NativeZoomLevel);
                                    DrawHUDMessages.Invoke();
                                    _spriteBatchEnd.Invoke();
                                }
                                if (CurrentEvent != null && CurrentEvent.skippable && !CurrentEvent.skipped)
                                {
                                    switch (activeClickableMenu)
                                    {
                                        case null:
                                        case MenuWithInventory _:
                                            break;
                                        default:
                                            SetSpriteBatchBeginNextID("A-G");
                                            SpriteBatchBegin.Invoke(NativeZoomLevel);
                                            CurrentEvent.DrawSkipButton(spriteBatch);
                                            _spriteBatchEnd.Invoke();
                                            break;
                                    }
                                }
                                DrawTutorialUI.Invoke();
                            }
                        }
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
