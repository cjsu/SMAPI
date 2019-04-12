
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
using StardewModdingAPI.Framework.StateTracking;
using StardewModdingAPI.Framework.Utilities;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Dimensions;
using xTile.Layers;
using SObject = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using StardewValley.Minigames;

namespace StardewModdingAPI.Framework
{
    /// <summary>SMAPI's extension of the game's core <see cref="Game1"/>, used to inject events.</summary>
    internal class SGame
    {
        /*********
        ** Fields
        *********/
        /****
        ** SMAPI state
        ****/
        /// <summary>Encapsulates monitoring and logging for SMAPI.</summary>
        private readonly IMonitor Monitor;

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

        internal bool OnObjectCanBePlacedHere(SObject instance, GameLocation location, Vector2 tile, ref bool result)
        {
            ObjectCanBePlacedHereEventArgs args = new ObjectCanBePlacedHereEventArgs(instance, location, tile, result);
            bool run =this.Events.ObjectCanBePlacedHere.RaiseForChainRun(args);
            result = args.__result;
            return run;
        }

        internal void OnObjectIsIndexOkForBasicShippedCategory(int index, ref bool result)
        {
            ObjectIsIndexOkForBasicShippedCategoryEventArgs args = new ObjectIsIndexOkForBasicShippedCategoryEventArgs(index, result);
            this.Events.ObjectIsIndexOkForBasicShippedCategory.RaiseForChainRun(args);
            result = args.__result;
        }

        internal bool OnObjectCheckForAction(SObject instance)
        {
            ObjectCheckForActionEventArgs args = new ObjectCheckForActionEventArgs(instance);
            bool run = this.Events.ObjectCheckForAction.RaiseForChainRun(args);
            return run;
        }

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        private bool IsBetweenSaveEvents;

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        private bool IsBetweenCreateEvents;

        /// <summary>A callback to invoke after the content language changes.</summary>
        private readonly Action OnLocaleChanged;

        /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
        public static Action OnLoadingFirstAsset;

        /// <summary>A callback to invoke after the game finishes initialising.</summary>
        private readonly Action OnGameInitialised;

        /// <summary>A callback to invoke when the game exits.</summary>
        private readonly Action OnGameExiting;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /****
        ** Game state
        ****/
        /// <summary>Monitors the entire game state for changes.</summary>
        private WatcherCore Watchers;

        /// <summary>Whether post-game-startup initialisation has been performed.</summary>
        private bool IsInitialised;

        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;

        public static void printLog(string msg)
        {
            SGame.instance.Monitor.Log(msg);
        }

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
        public SInputState Input => (SInputState)this.Reflection.GetField<InputState>(typeof(Game1), "input").GetValue();

        ///// <summary>The game's core multiplayer utility.</summary>
        //public SMultiplayer Multiplayer => (SMultiplayer)this.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

        /// <summary>A list of queued commands to execute.</summary>
        /// <remarks>This property must be threadsafe, since it's accessed from a separate console input thread.</remarks>
        public ConcurrentQueue<string> CommandQueue { get; } = new ConcurrentQueue<string>();

        private bool saveParsed;

        public static SGame instance;

        

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
        /// <param name="onLocaleChanged">A callback to invoke after the content language changes.</param>
        /// <param name="onGameInitialised">A callback to invoke after the game finishes initialising.</param>
        /// <param name="onGameExiting">A callback to invoke when the game exits.</param>
        internal SGame(ContentCoordinator contentCore, IMonitor monitor, IMonitor monitorForGame, Reflector reflection, EventManager eventManager, JsonHelper jsonHelper, ModRegistry modRegistry, DeprecationManager deprecationManager, Action onLocaleChanged, Action onGameInitialised, Action onGameExiting)
        {
            SGame.OnLoadingFirstAsset = SGame.ConstructorHack.OnLoadingFirstAsset;
            SGame.ConstructorHack = null;

            // check expectations
            if (contentCore == null)
                throw new InvalidOperationException($"The game didn't initialise its first content manager before SMAPI's {nameof(SGame)} constructor. This indicates an incompatible lifecycle change.");
            this.ContentCore = contentCore;

            // init XNA
            Game1.graphics.GraphicsProfile = GraphicsProfile.HiDef;

            // init SMAPI
            this.Monitor = monitor;
            this.MonitorForGame = monitorForGame;
            this.Events = eventManager;
            this.ModRegistry = modRegistry;
            this.Reflection = reflection;
            this.DeprecationManager = deprecationManager;
            this.OnLocaleChanged = onLocaleChanged;
            this.OnGameInitialised = onGameInitialised;
            this.OnGameExiting = onGameExiting;
            this.Reflection.GetField<InputState>(typeof(Game1), "input").SetValue(new SInputState());
            //this.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").SetValue(new SMultiplayer(monitor, eventManager, jsonHelper, modRegistry, reflection, this.OnModMessageReceived));
            //Game1.hooks = new SModHooks(this.OnNewDayAfterFade);

            // init observables
            //Game1.locations = new ObservableCollection<GameLocation>();
            this.saveParsed = false;
            SGame.instance = this;
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

        ///// <summary>Perform cleanup logic when the game exits.</summary>
        ///// <param name="sender">The event sender.</param>
        ///// <param name="args">The event args.</param>
        ///// <remarks>This overrides the logic in <see cref="Game1.exitEvent"/> to let SMAPI clean up before exit.</remarks>
        //protected override void OnExiting(object sender, EventArgs args)
        //{
        //    Game1.multiplayer.Disconnect();
        //    this.OnGameExiting?.Invoke();
        //}

        /// <summary>A callback invoked before <see cref="Game1.newDayAfterFade"/> runs.</summary>
        public void OnNewDayAfterFade()
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
                //this.Multiplayer.CleanupOnMultiplayerExit();
            }
            this.Monitor.VerboseLog($"Context: load stage changed to {newStage}");

            // raise events
            this.Events.LoadStageChanged.Raise(new LoadStageChangedEventArgs(oldStage, newStage));
            if (newStage == LoadStage.None)
                this.Events.ReturnedToTitle.RaiseEmpty();
        }

        ///// <summary>Constructor a content manager to read XNB files.</summary>
        ///// <param name="serviceProvider">The service provider to use to locate services.</param>
        ///// <param name="rootDirectory">The root directory to search for content.</param>
        //public LocalizedContentManager CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        //{
        //    // Game1._temporaryContent initialising from SGame constructor
        //    // NOTE: this method is called before the SGame constructor runs. Don't depend on anything being initialised at this point.
        //    if (this.ContentCore == null)
        //    {
        //        this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, SGame.ConstructorHack.Monitor, SGame.ConstructorHack.Reflection, SGame.ConstructorHack.JsonHelper, this.OnLoadingFirstAsset ?? SGame.ConstructorHack?.OnLoadingFirstAsset);
        //        this.NextContentManagerIsMain = true;
        //        return this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
        //    }

        //    // Game1.content initialising from LoadContent
        //    if (this.NextContentManagerIsMain)
        //    {
        //        this.NextContentManagerIsMain = false;
        //        return this.ContentCore.MainContentManager;
        //    }

        //    // any other content manager
        //    return this.ContentCore.CreateGameContentManager("(generated)");
        //}

        /// <summary>The method called when the game is updating its state. This happens roughly 60 times per second.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        public void Update(GameTime gameTime)
        {
            var events = this.Events;

            try
            {
                this.DeprecationManager.PrintQueued();

                /*********
                ** Special cases
                *********/
                // Perform first-tick initialisation.
                if (!this.IsInitialised)
                {
                    this.IsInitialised = true;
                    this.InitialiseAfterGameStarted();
                }

                // Abort if SMAPI is exiting.
                if (this.Monitor.IsExiting)
                {
                    this.Monitor.Log("SMAPI shutting down: aborting update.", LogLevel.Trace);
                    return;
                }

                // Run async tasks synchronously to avoid issues due to mod events triggering
                // concurrently with game code.
                if (Game1.currentLoader != null)
                {
                    int stage = Game1.currentLoader.Current;
                    switch (stage)
                    {
                        case 20 when (!this.saveParsed && SaveGame.loaded != null):
                            this.saveParsed = true;
                            this.OnLoadStageChanged(LoadStage.SaveParsed);
                            break;

                        case 36:
                            this.OnLoadStageChanged(LoadStage.SaveLoadedBasicInfo);
                            break;

                        case 50:
                            this.OnLoadStageChanged(LoadStage.SaveLoadedLocations);
                            break;

                        case 100:
                            Game1.currentLoader = null;
                            this.Monitor.Log("Game loader done.", LogLevel.Trace);
                            break;

                        default:
                            if (Game1.gameMode == Game1.playingGameMode)
                                this.OnLoadStageChanged(LoadStage.Preloaded);
                            break;
                    }
                    return;
                }
                Task _newDayTask = this.Reflection.GetField<Task>(typeof(Game1), "_newDayTask").GetValue();
                if (_newDayTask?.Status == TaskStatus.Created)
                {
                    this.Monitor.Log("New day task synchronising...", LogLevel.Trace);
                    _newDayTask.RunSynchronously();
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
                if (_newDayTask != null || Game1.gameMode == Game1.loadingMode)
                {
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    SGame.TicksElapsed++;
                    //Game1.game1.Update(gameTime);
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
                ** Update input
                *********/
                // This should *always* run, even when suppressing mod events, since the game uses
                // this too. For example, doing this after mod event suppression would prevent the
                // user from doing anything on the overnight shipping screen.
                SInputState inputState = this.Input;
                if (Game1.game1.IsActive)
                    inputState.TrueUpdate();

                /*********
                ** Save events + suppress events during save
                *********/
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
                    //Game1.game1.Update(gameTime);
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
                    return;
                }
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
                *********/
                this.Watchers.Update();

                /*********
                ** Locale changed events
                *********/
                if (this.Watchers.LocaleWatcher.IsChanged)
                {
                    this.Monitor.Log($"Context: locale set to {this.Watchers.LocaleWatcher.CurrentValue}.", LogLevel.Trace);
                    this.OnLocaleChanged();

                    this.Watchers.LocaleWatcher.Reset();
                }

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
                if (this.Watchers.WindowSizeWatcher.IsChanged)
                {
                    if (this.Monitor.IsVerbose)
                        this.Monitor.Log($"Events: window size changed to {this.Watchers.WindowSizeWatcher.CurrentValue}.", LogLevel.Trace);

                    Point oldSize = this.Watchers.WindowSizeWatcher.PreviousValue;
                    Point newSize = this.Watchers.WindowSizeWatcher.CurrentValue;

                    events.WindowResized.Raise(new WindowResizedEventArgs(oldSize, newSize));
                    this.Watchers.WindowSizeWatcher.Reset();
                }

                /*********
                ** Input events (if window has focus)
                *********/
                if (Game1.game1.IsActive)
                {
                    // raise events
                    bool isChatInput = Game1.IsChatting || (Context.IsMultiplayer && Context.IsWorldReady && Game1.activeClickableMenu == null && Game1.currentMinigame == null && inputState.IsAnyDown(Game1.options.chatButton));
                    if (!isChatInput)
                    {
                        ICursorPosition cursor = this.Input.CursorPosition;

                        // raise cursor moved event
                        if (this.Watchers.CursorWatcher.IsChanged)
                        {
                            if (events.CursorMoved.HasListeners())
                            {
                                ICursorPosition was = this.Watchers.CursorWatcher.PreviousValue;
                                ICursorPosition now = this.Watchers.CursorWatcher.CurrentValue;
                                this.Watchers.CursorWatcher.Reset();

                                events.CursorMoved.Raise(new CursorMovedEventArgs(was, now));
                            }
                            else
                                this.Watchers.CursorWatcher.Reset();
                        }

                        // raise mouse wheel scrolled
                        if (this.Watchers.MouseWheelScrollWatcher.IsChanged)
                        {
                            if (events.MouseWheelScrolled.HasListeners() || this.Monitor.IsVerbose)
                            {
                                int was = this.Watchers.MouseWheelScrollWatcher.PreviousValue;
                                int now = this.Watchers.MouseWheelScrollWatcher.CurrentValue;
                                this.Watchers.MouseWheelScrollWatcher.Reset();

                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Events: mouse wheel scrolled to {now}.", LogLevel.Trace);
                                events.MouseWheelScrolled.Raise(new MouseWheelScrolledEventArgs(cursor, was, now));
                            }
                            else
                                this.Watchers.MouseWheelScrollWatcher.Reset();
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
                if (this.Watchers.ActiveMenuWatcher.IsChanged)
                {
                    IClickableMenu was = this.Watchers.ActiveMenuWatcher.PreviousValue;
                    IClickableMenu now = this.Watchers.ActiveMenuWatcher.CurrentValue;
                    this.Watchers.ActiveMenuWatcher.Reset(); // reset here so a mod changing the menu will be raised as a new event afterwards

                    if (this.Monitor.IsVerbose)
                        this.Monitor.Log($"Context: menu changed from {was?.GetType().FullName ?? "none"} to {now?.GetType().FullName ?? "none"}.", LogLevel.Trace);

                    // raise menu events
                    events.MenuChanged.Raise(new MenuChangedEventArgs(was, now));
                    GameMenu gameMenu = now as GameMenu;
                    if (gameMenu != null)
                    {
                        foreach (IClickableMenu menuPage in gameMenu.pages)
                        {
                            OptionsPage optionsPage = menuPage as OptionsPage;
                            if (optionsPage != null)
                            {
                                List<OptionsElement> options = this.Reflection.GetField<List<OptionsElement>>(optionsPage, "options").GetValue();
                                foreach(IModMetadata modMetadata in this.ModRegistry.GetAll())
                                {
                                    if(modMetadata.Mod != null)
                                    {
                                        options.InsertRange(0, modMetadata.Mod.GetConfigMenuItems());
                                    }
                                }
                                this.Reflection.GetMethod(optionsPage, "updateContentPositions").Invoke();
                            }
                        }
                    }
                }

                /*********
                ** World & player events
                *********/
                if (Context.IsWorldReady)
                {
                    bool raiseWorldEvents = !this.Watchers.SaveIdWatcher.IsChanged; // don't report changes from unloaded => loaded

                    // raise location changes
                    if (this.Watchers.LocationsWatcher.IsChanged)
                    {
                        // location list changes
                        if (this.Watchers.LocationsWatcher.IsLocationListChanged)
                        {
                            GameLocation[] added = this.Watchers.LocationsWatcher.Added.ToArray();
                            GameLocation[] removed = this.Watchers.LocationsWatcher.Removed.ToArray();
                            this.Watchers.LocationsWatcher.ResetLocationList();

                            if (this.Monitor.IsVerbose)
                            {
                                string addedText = this.Watchers.LocationsWatcher.Added.Any() ? string.Join(", ", added.Select(p => p.Name)) : "none";
                                string removedText = this.Watchers.LocationsWatcher.Removed.Any() ? string.Join(", ", removed.Select(p => p.Name)) : "none";
                                this.Monitor.Log($"Context: location list changed (added {addedText}; removed {removedText}).", LogLevel.Trace);
                            }

                            events.LocationListChanged.Raise(new LocationListChangedEventArgs(added, removed));
                        }

                        // raise location contents changed
                        if (raiseWorldEvents)
                        {
                            foreach (LocationTracker watcher in this.Watchers.LocationsWatcher.Locations)
                            {
                                // buildings changed
                                if (watcher.BuildingsWatcher.IsChanged)
                                {
                                    GameLocation location = watcher.Location;
                                    Building[] added = watcher.BuildingsWatcher.Added.ToArray();
                                    Building[] removed = watcher.BuildingsWatcher.Removed.ToArray();
                                    watcher.BuildingsWatcher.Reset();

                                    events.BuildingListChanged.Raise(new BuildingListChangedEventArgs(location, added, removed));
                                }

                                // debris changed
                                if (watcher.DebrisWatcher.IsChanged)
                                {
                                    GameLocation location = watcher.Location;
                                    Debris[] added = watcher.DebrisWatcher.Added.ToArray();
                                    Debris[] removed = watcher.DebrisWatcher.Removed.ToArray();
                                    watcher.DebrisWatcher.Reset();

                                    events.DebrisListChanged.Raise(new DebrisListChangedEventArgs(location, added, removed));
                                }

                                // large terrain features changed
                                if (watcher.LargeTerrainFeaturesWatcher.IsChanged)
                                {
                                    GameLocation location = watcher.Location;
                                    LargeTerrainFeature[] added = watcher.LargeTerrainFeaturesWatcher.Added.ToArray();
                                    LargeTerrainFeature[] removed = watcher.LargeTerrainFeaturesWatcher.Removed.ToArray();
                                    watcher.LargeTerrainFeaturesWatcher.Reset();

                                    events.LargeTerrainFeatureListChanged.Raise(new LargeTerrainFeatureListChangedEventArgs(location, added, removed));
                                }

                                // NPCs changed
                                if (watcher.NpcsWatcher.IsChanged)
                                {
                                    GameLocation location = watcher.Location;
                                    NPC[] added = watcher.NpcsWatcher.Added.ToArray();
                                    NPC[] removed = watcher.NpcsWatcher.Removed.ToArray();
                                    watcher.NpcsWatcher.Reset();

                                    events.NpcListChanged.Raise(new NpcListChangedEventArgs(location, added, removed));
                                }

                                // objects changed
                                if (watcher.ObjectsWatcher.IsChanged)
                                {
                                    GameLocation location = watcher.Location;
                                    KeyValuePair<Vector2, SObject>[] added = watcher.ObjectsWatcher.Added.ToArray();
                                    KeyValuePair<Vector2, SObject>[] removed = watcher.ObjectsWatcher.Removed.ToArray();
                                    watcher.ObjectsWatcher.Reset();

                                    events.ObjectListChanged.Raise(new ObjectListChangedEventArgs(location, added, removed));
                                }

                                // terrain features changed
                                if (watcher.TerrainFeaturesWatcher.IsChanged)
                                {
                                    GameLocation location = watcher.Location;
                                    KeyValuePair<Vector2, TerrainFeature>[] added = watcher.TerrainFeaturesWatcher.Added.ToArray();
                                    KeyValuePair<Vector2, TerrainFeature>[] removed = watcher.TerrainFeaturesWatcher.Removed.ToArray();
                                    watcher.TerrainFeaturesWatcher.Reset();

                                    events.TerrainFeatureListChanged.Raise(new TerrainFeatureListChangedEventArgs(location, added, removed));
                                }
                            }
                        }
                        else
                            this.Watchers.LocationsWatcher.Reset();
                    }

                    // raise time changed
                    if (raiseWorldEvents && this.Watchers.TimeWatcher.IsChanged)
                    {
                        int was = this.Watchers.TimeWatcher.PreviousValue;
                        int now = this.Watchers.TimeWatcher.CurrentValue;
                        this.Watchers.TimeWatcher.Reset();

                        if (this.Monitor.IsVerbose)
                            this.Monitor.Log($"Events: time changed from {was} to {now}.", LogLevel.Trace);

                        events.TimeChanged.Raise(new TimeChangedEventArgs(was, now));
                    }
                    else
                        this.Watchers.TimeWatcher.Reset();

                    // raise player events
                    if (raiseWorldEvents)
                    {
                        PlayerTracker playerTracker = this.Watchers.CurrentPlayerTracker;

                        // raise current location changed
                        if (playerTracker.TryGetNewLocation(out GameLocation newLocation))
                        {
                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log($"Context: set location to {newLocation.Name}.", LogLevel.Trace);

                            GameLocation oldLocation = playerTracker.LocationWatcher.PreviousValue;
                            events.Warped.Raise(new WarpedEventArgs(playerTracker.Player, oldLocation, newLocation));
                        }

                        // raise player leveled up a skill
                        foreach (KeyValuePair<SkillType, IValueWatcher<int>> pair in playerTracker.GetChangedSkills())
                        {
                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log($"Events: player skill '{pair.Key}' changed from {pair.Value.PreviousValue} to {pair.Value.CurrentValue}.", LogLevel.Trace);

                            events.LevelChanged.Raise(new LevelChangedEventArgs(playerTracker.Player, pair.Key, pair.Value.PreviousValue, pair.Value.CurrentValue));
                        }

                        // raise player inventory changed
                        ItemStackChange[] changedItems = playerTracker.GetInventoryChanges().ToArray();
                        if (changedItems.Any())
                        {
                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log("Events: player inventory changed.", LogLevel.Trace);
                            events.InventoryChanged.Raise(new InventoryChangedEventArgs(playerTracker.Player, changedItems));
                        }

                        // raise mine level changed
                        if (playerTracker.TryGetNewMineLevel(out int mineLevel))
                        {
                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log($"Context: mine level changed to {mineLevel}.", LogLevel.Trace);
                        }
                    }
                    this.Watchers.CurrentPlayerTracker?.Reset();
                }

                // update save ID watcher
                this.Watchers.SaveIdWatcher.Reset();

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
                if (Context.IsSaveLoaded && Context.LoadStage != LoadStage.Loaded && Context.LoadStage != LoadStage.Ready)
                    this.OnLoadStageChanged(LoadStage.Loaded);

                // update tick
                bool isOneSecond = SGame.TicksElapsed % 60 == 0;
                events.UnvalidatedUpdateTicking.RaiseEmpty();
                events.UpdateTicking.RaiseEmpty();
                if (isOneSecond)
                    events.OneSecondUpdateTicking.RaiseEmpty();
                try
                {
                    this.Input.UpdateSuppression();
                    SGame.TicksElapsed++;
                    //Game1.game1.Update(gameTime);
                }
                catch (Exception ex)
                {
                    this.MonitorForGame.Log($"An error occured in the base update loop: {ex.GetLogSummary()}", LogLevel.Error);
                }
                events.UnvalidatedUpdateTicked.RaiseEmpty();
                events.UpdateTicked.RaiseEmpty();
                if (isOneSecond)
                    events.OneSecondUpdateTicked.RaiseEmpty();

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
                    this.Monitor.ExitGameImmediately("the game crashed when updating, and SMAPI was unable to recover the game.");
            }
        }

        /// <summary>The method called to draw everything to the screen.</summary>
        /// <param name="gameTime">A snapshot of the game timing state.</param>
        public void Draw(GameTime gameTime, RenderTarget2D toBuffer)
        {
            Context.IsInDrawLoop = true;
            try
            {
                this._draw(gameTime, toBuffer);
                this.DrawCrashTimer.Reset();
            }
            catch (Exception ex)
            {
                // log error
                this.Monitor.Log($"An error occured in the overridden draw loop: {ex.GetLogSummary()}", LogLevel.Error);

                // exit if irrecoverable
                if (!this.DrawCrashTimer.Decrement())
                {
                    this.Monitor.ExitGameImmediately("the game crashed when drawing, and SMAPI was unable to recover the game.");
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
        private void _draw(GameTime gameTime, RenderTarget2D toBuffer = null)
        {
            var events = this.Events;
            if (Game1.skipNextDrawCall)
            {
                Game1.skipNextDrawCall = false;
            }
            else
            {
                IReflectedField<bool> _drawActiveClickableMenu = this.Reflection.GetField<bool>(Game1.game1, "_drawActiveClickableMenu");
                IReflectedField<bool> _drawHUD = this.Reflection.GetField<bool>(Game1.game1, "_drawHUD");
                IReflectedField<Color> bgColor = this.Reflection.GetField<Color>(Game1.game1, "bgColor");
                IReflectedField<List<Farmer>> _farmerShadows = this.Reflection.GetField<List<Farmer>>(Game1.game1, "_farmerShadows");
                IReflectedField<StringBuilder> _debugStringBuilder = this.Reflection.GetField<StringBuilder>(typeof(Game1), "_debugStringBuilder");
                IReflectedField<BlendState> lightingBlend = this.Reflection.GetField<BlendState>(Game1.game1, "lightingBlend");
                _drawActiveClickableMenu.SetValue(false);
                _drawHUD.SetValue(false);

                IReflectedMethod renderScreenBuffer = this.Reflection.GetMethod(Game1.game1, "renderScreenBuffer", new Type[] { typeof(BlendState) });
                IReflectedMethod SpriteBatchBegin = this.Reflection.GetMethod(Game1.game1, "SpriteBatchBegin", new Type[] { typeof(float) });
                IReflectedMethod _spriteBatchBegin = this.Reflection.GetMethod(Game1.game1, "_spriteBatchBegin", new Type[] { typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState), typeof(Effect), typeof(Matrix) });
                IReflectedMethod _spriteBatchEnd = this.Reflection.GetMethod(Game1.game1, "_spriteBatchEnd", new Type[] { });
                IReflectedMethod drawOverlays = this.Reflection.GetMethod(Game1.game1, "drawOverlays", new Type[] { typeof(SpriteBatch) });
                IReflectedMethod DrawLoadingDotDotDot = this.Reflection.GetMethod(Game1.game1, "DrawLoadingDotDotDot", new Type[] { typeof(GameTime) });
                IReflectedMethod CheckToReloadGameLocationAfterDrawFail = this.Reflection.GetMethod(Game1.game1, "CheckToReloadGameLocationAfterDrawFail", new Type[] { typeof(string), typeof(Exception) });
                IReflectedMethod drawFarmBuildings = this.Reflection.GetMethod(Game1.game1, "drawFarmBuildings", new Type[] { });
                IReflectedMethod DrawTapToMoveTarget = this.Reflection.GetMethod(Game1.game1, "DrawTapToMoveTarget", new Type[] { });
                IReflectedMethod drawDialogueBox = this.Reflection.GetMethod(Game1.game1, "drawDialogueBox", new Type[] { });
                IReflectedMethod DrawDayTimeMoneyBox = this.Reflection.GetMethod(Game1.game1, "DrawDayTimeMoneyBox", new Type[] { });
                IReflectedMethod DrawHUD = this.Reflection.GetMethod(Game1.game1, "DrawHUD", new Type[] { });
                IReflectedMethod DrawAfterMap = this.Reflection.GetMethod(Game1.game1, "DrawAfterMap", new Type[] { });
                IReflectedMethod DrawToolbar = this.Reflection.GetMethod(Game1.game1, "DrawToolbar", new Type[] { });
                IReflectedMethod DrawVirtualJoypad = this.Reflection.GetMethod(Game1.game1, "DrawVirtualJoypad", new Type[] { });
                IReflectedMethod DrawFadeToBlackFullScreenRect = this.Reflection.GetMethod(Game1.game1, "DrawFadeToBlackFullScreenRect", new Type[] { });
                IReflectedMethod DrawChatBox = this.Reflection.GetMethod(Game1.game1, "DrawChatBox", new Type[] { });
                IReflectedMethod DrawDialogueBoxForPinchZoom = this.Reflection.GetMethod(Game1.game1, "DrawDialogueBoxForPinchZoom", new Type[] { });
                IReflectedMethod DrawUnscaledActiveClickableMenuForPinchZoom = this.Reflection.GetMethod(Game1.game1, "DrawUnscaledActiveClickableMenuForPinchZoom", new Type[] { });
                IReflectedMethod DrawNativeScaledActiveClickableMenuForPinchZoom = this.Reflection.GetMethod(Game1.game1, "DrawNativeScaledActiveClickableMenuForPinchZoom", new Type[] { });
                IReflectedMethod DrawHUDMessages = this.Reflection.GetMethod(Game1.game1, "DrawHUDMessages", new Type[] { });
                IReflectedMethod DrawTutorialUI = this.Reflection.GetMethod(Game1.game1, "DrawTutorialUI", new Type[] { });
                IReflectedMethod DrawGreenPlacementBounds = this.Reflection.GetMethod(Game1.game1, "DrawGreenPlacementBounds", new Type[] { });

                _drawHUD.SetValue(false);
                _drawActiveClickableMenu.SetValue(false);
                if (this.Reflection.GetField<Task>(typeof(Game1), "_newDayTask").GetValue() != null)
                {
                    Game1.game1.GraphicsDevice.Clear(bgColor.GetValue());
                }
                else
                {
                    if (Game1.options.zoomLevel != 1f)
                    {
                        if (toBuffer != null)
                        {
                            Game1.game1.GraphicsDevice.SetRenderTarget(toBuffer);
                        }
                        else
                        {
                            Game1.game1.GraphicsDevice.SetRenderTarget(Game1.game1.screen);
                        }
                    }
                    if (Game1.game1.IsSaving)
                    {
                        Game1.game1.GraphicsDevice.Clear(bgColor.GetValue());
                        renderScreenBuffer.Invoke(BlendState.Opaque);
                        if (Game1.activeClickableMenu != null)
                        {
                            if (Game1.IsActiveClickableMenuNativeScaled)
                            {
                                Game1.BackupViewportAndZoom(true);
                                Game1.SetSpriteBatchBeginNextID("A1");
                                SpriteBatchBegin.Invoke(Game1.NativeZoomLevel);
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
                                Game1.RestoreViewportAndZoom();
                            }
                            else
                            {
                                Game1.BackupViewportAndZoom(false);
                                Game1.SetSpriteBatchBeginNextID("A2");
                                SpriteBatchBegin.Invoke(1f);
                                Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                _spriteBatchEnd.Invoke();
                                Game1.RestoreViewportAndZoom();
                            }
                        }
                        if (Game1.overlayMenu != null)
                        {
                            Game1.BackupViewportAndZoom(false);
                            Game1.SetSpriteBatchBeginNextID("B");
                            _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                            Game1.overlayMenu.draw(Game1.spriteBatch);
                            _spriteBatchEnd.Invoke();
                            Game1.RestoreViewportAndZoom();
                        }
                    }
                    else
                    {
                        Game1.game1.GraphicsDevice.Clear(bgColor.GetValue());
                        if (((Game1.activeClickableMenu != null) && Game1.options.showMenuBackground) && Game1.activeClickableMenu.showWithoutTransparencyIfOptionIsSet())
                        {
                            Matrix matrix = Matrix.CreateScale((float)1f);
                            Game1.SetSpriteBatchBeginNextID("C");
                            _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, new Matrix?(matrix));
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
                            drawOverlays.Invoke(Game1.spriteBatch);
                            renderScreenBuffer.Invoke(BlendState.AlphaBlend);
                            if (Game1.overlayMenu != null)
                            {
                                Game1.SetSpriteBatchBeginNextID("D");
                                _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                                Game1.overlayMenu.draw(Game1.spriteBatch);
                                _spriteBatchEnd.Invoke();
                            }
                        }
                        else
                        {
                            Matrix? nullable;
                            if (Game1.emergencyLoading)
                            {
                                if (!Game1.SeenConcernedApeLogo)
                                {
                                    Game1.SetSpriteBatchBeginNextID("E");
                                    nullable = null;
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                    if (Game1.logoFadeTimer < 0x1388)
                                    {
                                        Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.White);
                                    }
                                    if (Game1.logoFadeTimer > 0x1194)
                                    {
                                        float num = Math.Min((float)1f, (float)(((float)(Game1.logoFadeTimer - 0x1194)) / 500f));
                                        Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * num);
                                    }
                                    Game1.spriteBatch.Draw(Game1.titleButtonsTexture, new Vector2((float)(Game1.viewport.Width / 2), (float)((Game1.viewport.Height / 2) - 90)), new Rectangle(0xab + ((((Game1.logoFadeTimer / 100) % 2) == 0) ? 0x6f : 0), 0x137, 0x6f, 60), Color.White * ((Game1.logoFadeTimer < 500) ? (((float)Game1.logoFadeTimer) / 500f) : ((Game1.logoFadeTimer > 0x1194) ? (1f - (((float)(Game1.logoFadeTimer - 0x1194)) / 500f)) : 1f)), 0f, Vector2.Zero, (float)3f, SpriteEffects.None, 0.2f);
                                    Game1.spriteBatch.Draw(Game1.titleButtonsTexture, new Vector2((float)((Game1.viewport.Width / 2) - 0x105), (float)((Game1.viewport.Height / 2) - 0x66)), new Rectangle((((Game1.logoFadeTimer / 100) % 2) == 0) ? 0x55 : 0, 0x132, 0x55, 0x45), Color.White * ((Game1.logoFadeTimer < 500) ? (((float)Game1.logoFadeTimer) / 500f) : ((Game1.logoFadeTimer > 0x1194) ? (1f - (((float)(Game1.logoFadeTimer - 0x1194)) / 500f)) : 1f)), 0f, Vector2.Zero, (float)3f, SpriteEffects.None, 0.2f);
                                    _spriteBatchEnd.Invoke();
                                }
                                Game1.logoFadeTimer -= gameTime.ElapsedGameTime.Milliseconds;
                            }
                            if (Game1.gameMode == 11)
                            {
                                Game1.SetSpriteBatchBeginNextID("F");
                                _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                                events.Rendering.RaiseEmpty();
                                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.content.LoadString(@"Strings\StringsFromCSFiles:Game1.cs.3685"), new Vector2(16f, 16f), Color.HotPink);
                                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.content.LoadString(@"Strings\StringsFromCSFiles:Game1.cs.3686"), new Vector2(16f, 32f), new Color(0, 0xff, 0));
                                Game1.spriteBatch.DrawString(Game1.dialogueFont, Game1.parseText(Game1.errorMessage, Game1.dialogueFont, Game1.graphics.GraphicsDevice.Viewport.Width, 1f), new Vector2(16f, 48f), Color.White);
                                events.Rendered.RaiseEmpty();
                                _spriteBatchEnd.Invoke();
                            }
                            else if (Game1.currentMinigame != null)
                            {
                                Game1.currentMinigame.draw(Game1.spriteBatch);
                                if ((Game1.globalFade && !Game1.menuUp) && (!Game1.nameSelectUp || Game1.messagePause))
                                {
                                    Game1.SetSpriteBatchBeginNextID("G");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((Game1.gameMode == 0) ? (1f - Game1.fadeToBlackAlpha) : Game1.fadeToBlackAlpha));
                                    _spriteBatchEnd.Invoke();
                                }
                                drawOverlays.Invoke(Game1.spriteBatch);
                                renderScreenBuffer.Invoke(BlendState.AlphaBlend);
                                if (((Game1.currentMinigame is FishingGame) || (Game1.currentMinigame is FantasyBoardGame)) && (Game1.activeClickableMenu != null))
                                {
                                    Game1.SetSpriteBatchBeginNextID("A-A");
                                    SpriteBatchBegin.Invoke(1f);
                                    Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                    _spriteBatchEnd.Invoke();
                                    drawOverlays.Invoke(Game1.spriteBatch);
                                }
                            }
                            else if (Game1.showingEndOfNightStuff)
                            {
                                renderScreenBuffer.Invoke(BlendState.Opaque);
                                Game1.BackupViewportAndZoom(true);
                                Game1.SetSpriteBatchBeginNextID("A-B");
                                SpriteBatchBegin.Invoke(Game1.NativeZoomLevel);
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
                                _spriteBatchEnd.Invoke();
                                drawOverlays.Invoke(Game1.spriteBatch);
                                Game1.RestoreViewportAndZoom();
                            }
                            else if ((Game1.gameMode == 6) || ((Game1.gameMode == 3) && (Game1.currentLocation == null)))
                            {
                                events.Rendering.RaiseEmpty();
                                DrawLoadingDotDotDot.Invoke(gameTime);
                                events.Rendered.RaiseEmpty();
                                drawOverlays.Invoke(Game1.spriteBatch);
                                renderScreenBuffer.Invoke(BlendState.AlphaBlend);
                                if (Game1.overlayMenu != null)
                                {
                                    Game1.SetSpriteBatchBeginNextID("H");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                                    Game1.overlayMenu.draw(Game1.spriteBatch);
                                    _spriteBatchEnd.Invoke();
                                }
                                //base.Draw(gameTime);
                            }
                            else
                            {
                                byte batchOpens = 0; // used for rendering event
                                if (Game1.gameMode == 0)
                                {
                                    Game1.SetSpriteBatchBeginNextID("I");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                                    if (++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                }
                                else if (!Game1.drawGame)
                                {
                                    Game1.SetSpriteBatchBeginNextID("J");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, null);
                                    if (++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                }
                                else if (Game1.drawGame)
                                {
                                    if (Game1.drawLighting)
                                    {
                                        Game1.game1.GraphicsDevice.SetRenderTarget(Game1.lightmap);
                                        Game1.game1.GraphicsDevice.Clear(Color.White * 0f);
                                        Game1.SetSpriteBatchBeginNextID("K");
                                        nullable = null;
                                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, nullable);
                                        if (++batchOpens == 1)
                                            events.Rendering.RaiseEmpty();
                                        Game1.spriteBatch.Draw(Game1.staminaRect, Game1.lightmap.Bounds, Game1.currentLocation.Name.StartsWith("UndergroundMine") ? Game1.mine.getLightingColor(gameTime) : ((!Game1.ambientLight.Equals(Color.White) && (!RainManager.Instance.isRaining || (Game1.currentLocation.isOutdoors == null))) ? Game1.ambientLight : Game1.outdoorLight));
                                        for (int i = 0; i < Game1.currentLightSources.Count; i++)
                                        {
                                            if (Utility.isOnScreen((Vector2)Game1.currentLightSources.ElementAt<LightSource>(i).position, (int)((Game1.currentLightSources.ElementAt<LightSource>(i).radius * 64f) * 4f)))
                                            {
                                                Game1.spriteBatch.Draw(Game1.currentLightSources.ElementAt<LightSource>(i).lightTexture, Game1.GlobalToLocal(Game1.viewport, (Vector2)Game1.currentLightSources.ElementAt<LightSource>(i).position) / ((float)(Game1.options.lightingQuality / 2)), new Rectangle?(Game1.currentLightSources.ElementAt<LightSource>(i).lightTexture.Bounds), (Color)Game1.currentLightSources.ElementAt<LightSource>(i).color, 0f, new Vector2((float)Game1.currentLightSources.ElementAt<LightSource>(i).lightTexture.Bounds.Center.X, (float)Game1.currentLightSources.ElementAt<LightSource>(i).lightTexture.Bounds.Center.Y), (float)(Game1.currentLightSources.ElementAt<LightSource>(i).radius / ((float)(Game1.options.lightingQuality / 2))), SpriteEffects.None, 0.9f);
                                            }
                                        }
                                        _spriteBatchEnd.Invoke();
                                        Game1.game1.GraphicsDevice.SetRenderTarget((Game1.options.zoomLevel == 1f) ? null : Game1.game1.screen);
                                    }
                                    if (Game1.bloomDay && (Game1.bloom != null))
                                    {
                                        Game1.bloom.BeginDraw();
                                    }
                                    Game1.game1.GraphicsDevice.Clear(bgColor.GetValue());
                                    Game1.SetSpriteBatchBeginNextID("L");
                                    nullable = null;
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                    if (++batchOpens == 1)
                                        events.Rendering.RaiseEmpty();
                                    events.RenderingWorld.RaiseEmpty();
                                    if (Game1.background != null)
                                    {
                                        Game1.background.draw(Game1.spriteBatch);
                                    }
                                    Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                                    try
                                    {
                                        Game1.currentLocation.Map.GetLayer("Back").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                    }
                                    catch (KeyNotFoundException exception)
                                    {
                                        CheckToReloadGameLocationAfterDrawFail.Invoke("Back", exception);
                                    }
                                    Game1.currentLocation.drawWater(Game1.spriteBatch);
                                    _farmerShadows.GetValue().Clear();
                                    if (((Game1.currentLocation.currentEvent != null) && !Game1.currentLocation.currentEvent.isFestival) && (Game1.currentLocation.currentEvent.farmerActors.Count > 0))
                                    {
                                        foreach (Farmer farmer in Game1.currentLocation.currentEvent.farmerActors)
                                        {
                                            if ((farmer.IsLocalPlayer && Game1.displayFarmer) || (farmer.hidden == null))
                                            {
                                                _farmerShadows.GetValue().Add(farmer);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (Farmer farmer2 in Game1.currentLocation.farmers)
                                        {
                                            if ((farmer2.IsLocalPlayer && Game1.displayFarmer) || (farmer2.hidden == null))
                                            {
                                                _farmerShadows.GetValue().Add(farmer2);
                                            }
                                        }
                                    }
                                    if (!Game1.currentLocation.shouldHideCharacters())
                                    {
                                        if (Game1.CurrentEvent == null)
                                        {
                                            foreach (NPC npc in Game1.currentLocation.characters)
                                            {
                                                if (((npc.swimming == null) && !npc.HideShadow) && (!npc.IsInvisible && !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(npc.getTileLocation())))
                                                {
                                                    Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, npc.Position + new Vector2(((float)(npc.Sprite.SpriteWidth * 4)) / 2f, (float)(npc.GetBoundingBox().Height + (npc.IsMonster ? 0 : 12)))), new Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)((4f + (((float)npc.yJumpOffset) / 40f)) * npc.scale), SpriteEffects.None, Math.Max((float)0f, (float)(((float)npc.getStandingY()) / 10000f)) - 1E-06f);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (NPC npc2 in Game1.CurrentEvent.actors)
                                            {
                                                if (((npc2.swimming == null) && !npc2.HideShadow) && !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(npc2.getTileLocation()))
                                                {
                                                    Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, npc2.Position + new Vector2(((float)(npc2.Sprite.SpriteWidth * 4)) / 2f, (float)(npc2.GetBoundingBox().Height + (npc2.IsMonster ? 0 : ((npc2.Sprite.SpriteHeight <= 0x10) ? -4 : 12))))), new Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)((4f + (((float)npc2.yJumpOffset) / 40f)) * npc2.scale), SpriteEffects.None, Math.Max((float)0f, (float)(((float)npc2.getStandingY()) / 10000f)) - 1E-06f);
                                                }
                                            }
                                        }
                                        foreach (Farmer farmer3 in _farmerShadows.GetValue())
                                        {
                                            if (((farmer3.swimming == null) && !farmer3.isRidingHorse()) && ((Game1.currentLocation == null) || !Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmer3.getTileLocation())))
                                            {
                                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(farmer3.Position + new Vector2(32f, 24f)), new Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)(4f - (((farmer3.running || farmer3.UsingTool) && (farmer3.FarmerSprite.currentAnimationIndex > 1)) ? (Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmer3.FarmerSprite.CurrentFrame]) * 0.5f) : 0f)), SpriteEffects.None, 0f);
                                            }
                                        }
                                    }
                                    try
                                    {
                                        Game1.currentLocation.Map.GetLayer("Buildings").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                    }
                                    catch (KeyNotFoundException exception2)
                                    {
                                        CheckToReloadGameLocationAfterDrawFail.Invoke("Buildings", exception2);
                                    }
                                    Game1.mapDisplayDevice.EndScene();
                                    if (Game1.currentLocation.tapToMove.targetNPC != null)
                                    {
                                        Game1.spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, Game1.currentLocation.tapToMove.targetNPC.Position + new Vector2((((float)(Game1.currentLocation.tapToMove.targetNPC.Sprite.SpriteWidth * 4)) / 2f) - 32f, (float)((Game1.currentLocation.tapToMove.targetNPC.GetBoundingBox().Height + (Game1.currentLocation.tapToMove.targetNPC.IsMonster ? 0 : 12)) - 0x20))), new Rectangle(0xc2, 0x184, 0x10, 0x10), Color.White, 0f, Vector2.Zero, (float)4f, SpriteEffects.None, 0.58f);
                                    }
                                    _spriteBatchEnd.Invoke();
                                    Game1.SetSpriteBatchBeginNextID("M");
                                    nullable = null;
                                    _spriteBatchBegin.Invoke(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                    if (!Game1.currentLocation.shouldHideCharacters())
                                    {
                                        if (Game1.CurrentEvent == null)
                                        {
                                            foreach (NPC npc3 in Game1.currentLocation.characters)
                                            {
                                                if (((npc3.swimming == null) && !npc3.HideShadow) && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(npc3.getTileLocation()))
                                                {
                                                    Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, npc3.Position + new Vector2(((float)(npc3.Sprite.SpriteWidth * 4)) / 2f, (float)(npc3.GetBoundingBox().Height + (npc3.IsMonster ? 0 : 12)))), new Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)((4f + (((float)npc3.yJumpOffset) / 40f)) * npc3.scale), SpriteEffects.None, Math.Max((float)0f, (float)(((float)npc3.getStandingY()) / 10000f)) - 1E-06f);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (NPC npc4 in Game1.CurrentEvent.actors)
                                            {
                                                if (((npc4.swimming == null) && !npc4.HideShadow) && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(npc4.getTileLocation()))
                                                {
                                                    Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, npc4.Position + new Vector2(((float)(npc4.Sprite.SpriteWidth * 4)) / 2f, (float)(npc4.GetBoundingBox().Height + (npc4.IsMonster ? 0 : 12)))), new Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)((4f + (((float)npc4.yJumpOffset) / 40f)) * npc4.scale), SpriteEffects.None, Math.Max((float)0f, (float)(((float)npc4.getStandingY()) / 10000f)) - 1E-06f);
                                                }
                                            }
                                        }
                                        foreach (Farmer farmer4 in _farmerShadows.GetValue())
                                        {
                                            if (((farmer4.swimming == null) && !farmer4.isRidingHorse()) && ((Game1.currentLocation != null) && Game1.currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmer4.getTileLocation())))
                                            {
                                                Game1.spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(farmer4.Position + new Vector2(32f, 24f)), new Rectangle?(Game1.shadowTexture.Bounds), Color.White, 0f, new Vector2((float)Game1.shadowTexture.Bounds.Center.X, (float)Game1.shadowTexture.Bounds.Center.Y), (float)(4f - (((farmer4.running || farmer4.UsingTool) && (farmer4.FarmerSprite.currentAnimationIndex > 1)) ? (Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmer4.FarmerSprite.CurrentFrame]) * 0.5f) : 0f)), SpriteEffects.None, 0f);
                                            }
                                        }
                                    }
                                    if ((Game1.eventUp || Game1.killScreen) && (!Game1.killScreen && (Game1.currentLocation.currentEvent != null)))
                                    {
                                        Game1.currentLocation.currentEvent.draw(Game1.spriteBatch);
                                    }
                                    if (((Game1.player.currentUpgrade != null) && (Game1.player.currentUpgrade.daysLeftTillUpgradeDone <= 3)) && Game1.currentLocation.Name.Equals("Farm"))
                                    {
                                        Game1.spriteBatch.Draw(Game1.player.currentUpgrade.workerTexture, Game1.GlobalToLocal(Game1.viewport, Game1.player.currentUpgrade.positionOfCarpenter), new Rectangle?(Game1.player.currentUpgrade.getSourceRectangle()), Color.White, 0f, Vector2.Zero, (float)1f, SpriteEffects.None, (Game1.player.currentUpgrade.positionOfCarpenter.Y + 48f) / 10000f);
                                    }
                                    Game1.currentLocation.draw(Game1.spriteBatch);
                                    if (((Game1.player.ActiveObject == null) && (Game1.player.UsingTool || Game1.pickingTool)) && ((Game1.player.CurrentTool != null) && (!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool)))
                                    {
                                        Game1.drawTool(Game1.player);
                                    }
                                    if (Game1.currentLocation.Name.Equals("Farm"))
                                    {
                                        drawFarmBuildings.Invoke();
                                    }
                                    if (Game1.tvStation >= 0)
                                    {
                                        Game1.spriteBatch.Draw(Game1.tvStationTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(400f, 160f)), new Rectangle(Game1.tvStation * 0x18, 0, 0x18, 15), Color.White, 0f, Vector2.Zero, (float)4f, SpriteEffects.None, 1E-08f);
                                    }
                                    if (Game1.panMode)
                                    {
                                        Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle((((int)Math.Floor((double)(((double)(Game1.getOldMouseX() + Game1.viewport.X)) / 64.0))) * 0x40) - Game1.viewport.X, (((int)Math.Floor((double)(((double)(Game1.getOldMouseY() + Game1.viewport.Y)) / 64.0))) * 0x40) - Game1.viewport.Y, 0x40, 0x40), Color.Lime * 0.75f);
                                        foreach (Warp warp in Game1.currentLocation.warps)
                                        {
                                            Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle((warp.X * 0x40) - Game1.viewport.X, (warp.Y * 0x40) - Game1.viewport.Y, 0x40, 0x40), Color.Red * 0.75f);
                                        }
                                    }
                                    Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                                    try
                                    {
                                        Game1.currentLocation.Map.GetLayer("Front").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                    }
                                    catch (KeyNotFoundException exception3)
                                    {
                                        CheckToReloadGameLocationAfterDrawFail.Invoke("Front", exception3);
                                    }
                                    Game1.mapDisplayDevice.EndScene();
                                    Game1.currentLocation.drawAboveFrontLayer(Game1.spriteBatch);
                                    if ((((Game1.currentLocation.tapToMove.targetNPC == null) && (Game1.displayHUD || Game1.eventUp)) && (((Game1.currentBillboard == 0) && (Game1.gameMode == 3)) && (!Game1.freezeControls && !Game1.panMode))) && !Game1.HostPaused)
                                    {
                                        DrawTapToMoveTarget.Invoke();
                                    }
                                    _spriteBatchEnd.Invoke();
                                    Game1.SetSpriteBatchBeginNextID("N");
                                    nullable = null;
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                    if (((Game1.displayFarmer && (Game1.player.ActiveObject != null)) && ((Game1.player.ActiveObject.bigCraftable != null) && Game1.game1.checkBigCraftableBoundariesForFrontLayer())) && (Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null))
                                    {
                                        Game1.drawPlayerHeldObject(Game1.player);
                                    }
                                    else if ((Game1.displayFarmer && (Game1.player.ActiveObject != null)) && (((Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int)Game1.player.Position.X, ((int)Game1.player.Position.Y) - 0x26), Game1.viewport.Size) != null) && !Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location((int)Game1.player.Position.X, ((int)Game1.player.Position.Y) - 0x26), Game1.viewport.Size).TileIndexProperties.ContainsKey("FrontAlways")) || ((Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.GetBoundingBox().Right, ((int)Game1.player.Position.Y) - 0x26), Game1.viewport.Size) != null) && !Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.GetBoundingBox().Right, ((int)Game1.player.Position.Y) - 0x26), Game1.viewport.Size).TileIndexProperties.ContainsKey("FrontAlways"))))
                                    {
                                        Game1.drawPlayerHeldObject(Game1.player);
                                    }
                                    if (((Game1.player.UsingTool || Game1.pickingTool) && (Game1.player.CurrentTool != null)) && ((!Game1.player.CurrentTool.Name.Equals("Seeds") || Game1.pickingTool) && ((Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), ((int)Game1.player.Position.Y) - 0x26), Game1.viewport.Size) != null) && (Game1.currentLocation.Map.GetLayer("Front").PickTile(new Location(Game1.player.getStandingX(), Game1.player.getStandingY()), Game1.viewport.Size) == null))))
                                    {
                                        Game1.drawTool(Game1.player);
                                    }
                                    if (Game1.currentLocation.Map.GetLayer("AlwaysFront") != null)
                                    {
                                        Game1.mapDisplayDevice.BeginScene(Game1.spriteBatch);
                                        try
                                        {
                                            Game1.currentLocation.Map.GetLayer("AlwaysFront").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, false, 4);
                                        }
                                        catch (KeyNotFoundException exception4)
                                        {
                                            CheckToReloadGameLocationAfterDrawFail.Invoke("AlwaysFront", exception4);
                                        }
                                        Game1.mapDisplayDevice.EndScene();
                                    }
                                    if (((Game1.toolHold > 400f) && (Game1.player.CurrentTool.UpgradeLevel >= 1)) && Game1.player.canReleaseTool)
                                    {
                                        Color white = Color.White;
                                        switch ((((int)(Game1.toolHold / 600f)) + 2))
                                        {
                                            case 1:
                                                white = Tool.copperColor;
                                                break;

                                            case 2:
                                                white = Tool.steelColor;
                                                break;

                                            case 3:
                                                white = Tool.goldColor;
                                                break;

                                            case 4:
                                                white = Tool.iridiumColor;
                                                break;
                                        }
                                        Game1.spriteBatch.Draw(Game1.littleEffect, new Rectangle(((int)Game1.player.getLocalPosition(Game1.viewport).X) - 2, (((int)Game1.player.getLocalPosition(Game1.viewport).Y) - (Game1.player.CurrentTool.Name.Equals("Watering Can") ? 0 : 0x40)) - 2, ((int)((Game1.toolHold % 600f) * 0.08f)) + 4, 12), Color.Black);
                                        Game1.spriteBatch.Draw(Game1.littleEffect, new Rectangle((int)Game1.player.getLocalPosition(Game1.viewport).X, ((int)Game1.player.getLocalPosition(Game1.viewport).Y) - (Game1.player.CurrentTool.Name.Equals("Watering Can") ? 0 : 0x40), (int)((Game1.toolHold % 600f) * 0.08f), 8), white);
                                    }
                                    if ((WeatherDebrisManager.Instance.isDebrisWeather && Game1.currentLocation.IsOutdoors) && ((Game1.currentLocation.ignoreDebrisWeather == null) && !Game1.currentLocation.Name.Equals("Desert")))
                                    {
                                        WeatherDebrisManager.Instance.Draw(Game1.spriteBatch);
                                    }
                                    if (Game1.farmEvent != null)
                                    {
                                        Game1.farmEvent.draw(Game1.spriteBatch);
                                    }
                                    if ((Game1.currentLocation.LightLevel > 0f) && (Game1.timeOfDay < 0x7d0))
                                    {
                                        Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * Game1.currentLocation.LightLevel);
                                    }
                                    if (Game1.screenGlow)
                                    {
                                        Game1.spriteBatch.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Game1.screenGlowColor * Game1.screenGlowAlpha);
                                    }
                                    Game1.currentLocation.drawAboveAlwaysFrontLayer(Game1.spriteBatch);
                                    if (((Game1.player.CurrentTool != null) && (Game1.player.CurrentTool is FishingRod)) && (((Game1.player.CurrentTool as FishingRod).isTimingCast || ((Game1.player.CurrentTool as FishingRod).castingChosenCountdown > 0f)) || ((Game1.player.CurrentTool as FishingRod).fishCaught || (Game1.player.CurrentTool as FishingRod).showingTreasure)))
                                    {
                                        Game1.player.CurrentTool.draw(Game1.spriteBatch);
                                    }
                                    if (((RainManager.Instance.isRaining && Game1.currentLocation.IsOutdoors) && (!Game1.currentLocation.Name.Equals("Desert") && !(Game1.currentLocation is Summit))) && (!Game1.eventUp || Game1.currentLocation.isTileOnMap(new Vector2((float)(Game1.viewport.X / 0x40), (float)(Game1.viewport.Y / 0x40)))))
                                    {
                                        RainManager.Instance.Draw(Game1.spriteBatch);
                                    }
                                    _spriteBatchEnd.Invoke();
                                    Game1.SetSpriteBatchBeginNextID("O");
                                    nullable = null;
                                    _spriteBatchBegin.Invoke(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, nullable);
                                    if (Game1.eventUp && (Game1.currentLocation.currentEvent != null))
                                    {
                                        Game1.currentLocation.currentEvent.drawAboveAlwaysFrontLayer(Game1.spriteBatch);
                                        foreach (NPC npc5 in Game1.currentLocation.currentEvent.actors)
                                        {
                                            if (npc5.isEmoting)
                                            {
                                                Vector2 position = npc5.getLocalPosition(Game1.viewport);
                                                position.Y -= 140f;
                                                if (npc5.Age == 2)
                                                {
                                                    position.Y += 32f;
                                                }
                                                else if (npc5.Gender == 1)
                                                {
                                                    position.Y += 10f;
                                                }
                                                Game1.spriteBatch.Draw(Game1.emoteSpriteSheet, position, new Rectangle((npc5.CurrentEmoteIndex * 0x10) % Game1.emoteSpriteSheet.Width, ((npc5.CurrentEmoteIndex * 0x10) / Game1.emoteSpriteSheet.Width) * 0x10, 0x10, 0x10), Color.White, 0f, Vector2.Zero, (float)4f, SpriteEffects.None, ((float)npc5.getStandingY()) / 10000f);
                                            }
                                        }
                                    }
                                    _spriteBatchEnd.Invoke();
                                    if (Game1.drawLighting)
                                    {
                                        Game1.SetSpriteBatchBeginNextID("P");
                                        nullable = null;
                                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, lightingBlend.GetValue(), SamplerState.LinearClamp, null, null, null, nullable);
                                        Game1.spriteBatch.Draw(Game1.lightmap, Vector2.Zero, new Rectangle?(Game1.lightmap.Bounds), Color.White, 0f, Vector2.Zero, (float)(Game1.options.lightingQuality / 2), SpriteEffects.None, 1f);
                                        if ((RainManager.Instance.isRaining && (Game1.currentLocation.isOutdoors != null)) && !(Game1.currentLocation is Desert))
                                        {
                                            Game1.spriteBatch.Draw(Game1.staminaRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.OrangeRed * 0.45f);
                                        }
                                        _spriteBatchEnd.Invoke();
                                    }
                                    Game1.SetSpriteBatchBeginNextID("Q");
                                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                                    events.RenderedWorld.RaiseEmpty();
                                    if (Game1.drawGrid)
                                    {
                                        int x = -Game1.viewport.X % 0x40;
                                        float num5 = -Game1.viewport.Y % 0x40;
                                        for (int i = x; i < Game1.graphics.GraphicsDevice.Viewport.Width; i += 0x40)
                                        {
                                            Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(i, (int)num5, 1, Game1.graphics.GraphicsDevice.Viewport.Height), Color.Red * 0.5f);
                                        }
                                        for (float j = num5; j < Game1.graphics.GraphicsDevice.Viewport.Height; j += 64f)
                                        {
                                            Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle(x, (int)j, Game1.graphics.GraphicsDevice.Viewport.Width, 1), Color.Red * 0.5f);
                                        }
                                    }
                                    if (((Game1.displayHUD || Game1.eventUp) && ((Game1.currentBillboard == 0) && (Game1.gameMode == 3))) && ((!Game1.freezeControls && !Game1.panMode) && !Game1.HostPaused))
                                    {
                                        _drawHUD.SetValue(true);
                                        if (Game1.isOutdoorMapSmallerThanViewport())
                                        {
                                            Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, -Math.Min(Game1.viewport.X, 0x1000), Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black);
                                            Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle(-Game1.viewport.X + (Game1.currentLocation.map.Layers[0].LayerWidth * 0x40), 0, Math.Min(0x1000, Game1.graphics.GraphicsDevice.Viewport.Width - (-Game1.viewport.X + (Game1.currentLocation.map.Layers[0].LayerWidth * 0x40))), Game1.graphics.GraphicsDevice.Viewport.Height), Color.Black);
                                        }
                                        DrawGreenPlacementBounds.Invoke();
                                    }
                                }
                                if (Game1.farmEvent != null)
                                {
                                    Game1.farmEvent.draw(Game1.spriteBatch);
                                }
                                if (((Game1.dialogueUp && !Game1.nameSelectUp) && !Game1.messagePause) && ((Game1.activeClickableMenu == null) || !(Game1.activeClickableMenu is DialogueBox)))
                                {
                                    drawDialogueBox.Invoke();
                                }
                                if (Game1.progressBar)
                                {
                                    Game1.spriteBatch.Draw(Game1.fadeToBlackRect, new Rectangle((Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 0x80, Game1.dialogueWidth, 0x20), Color.LightGray);
                                    Game1.spriteBatch.Draw(Game1.staminaRect, new Rectangle((Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - Game1.dialogueWidth) / 2, Game1.graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 0x80, (int)((Game1.pauseAccumulator / Game1.pauseTime) * Game1.dialogueWidth), 0x20), Color.DimGray);
                                }
                                if ((RainManager.Instance.isRaining && (Game1.currentLocation != null)) && ((Game1.currentLocation.isOutdoors != null) && !(Game1.currentLocation is Desert)))
                                {
                                    Game1.spriteBatch.Draw(Game1.staminaRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Blue * 0.2f);
                                }
                                if ((Game1.messagePause || Game1.globalFade) && Game1.dialogueUp)
                                {
                                    drawDialogueBox.Invoke();
                                }
                                using (List<TemporaryAnimatedSprite>.Enumerator enumerator6 = Game1.screenOverlayTempSprites.GetEnumerator())
                                {
                                    while (enumerator6.MoveNext())
                                    {
                                        enumerator6.Current.draw(Game1.spriteBatch, true, 0, 0, 1f);
                                    }
                                }
                                if (Game1.debugMode)
                                {
                                    StringBuilder text = _debugStringBuilder.GetValue();
                                    text.Clear();
                                    if (Game1.panMode)
                                    {
                                        text.Append((int)((Game1.getOldMouseX() + Game1.viewport.X) / 0x40));
                                        text.Append(",");
                                        text.Append((int)((Game1.getOldMouseY() + Game1.viewport.Y) / 0x40));
                                    }
                                    else
                                    {
                                        text.Append("Game1.player: ");
                                        text.Append((int)(Game1.player.getStandingX() / 0x40));
                                        text.Append(", ");
                                        text.Append((int)(Game1.player.getStandingY() / 0x40));
                                    }
                                    text.Append(" mouseTransparency: ");
                                    text.Append(Game1.mouseCursorTransparency);
                                    text.Append(" mousePosition: ");
                                    text.Append(Game1.getMouseX());
                                    text.Append(",");
                                    text.Append(Game1.getMouseY());
                                    text.Append(Environment.NewLine);
                                    text.Append("debugOutput: ");
                                    text.Append(Game1.debugOutput);
                                    Game1.spriteBatch.DrawString(Game1.smallFont, text, new Vector2((float)Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea().X, (float)(Game1.game1.GraphicsDevice.Viewport.GetTitleSafeArea().Y + (Game1.smallFont.LineSpacing * 8))), Color.Red, 0f, Vector2.Zero, (float)1f, SpriteEffects.None, 0.09999999f);
                                }
                                if (Game1.showKeyHelp)
                                {
                                    Game1.spriteBatch.DrawString(Game1.smallFont, Game1.keyHelpString, new Vector2(64f, ((Game1.viewport.Height - 0x40) - (Game1.dialogueUp ? (0xc0 + (Game1.isQuestion ? (Game1.questionChoices.Count * 0x40) : 0)) : 0)) - Game1.smallFont.MeasureString(Game1.keyHelpString).Y), Color.LightGray, 0f, Vector2.Zero, (float)1f, SpriteEffects.None, 0.9999999f);
                                }
                                if (Game1.activeClickableMenu != null)
                                {
                                    try
                                    {
                                        _drawActiveClickableMenu.SetValue(true);
                                        events.RenderingActiveMenu.RaiseEmpty();
                                        if (Game1.activeClickableMenu is CarpenterMenu)
                                        {
                                            ((CarpenterMenu)Game1.activeClickableMenu).DrawPlacementSquares(Game1.spriteBatch);
                                        }
                                        else if (Game1.activeClickableMenu is MuseumMenu)
                                        {
                                            ((MuseumMenu)Game1.activeClickableMenu).DrawPlacementGrid(Game1.spriteBatch);
                                        }
                                        if (!Game1.IsActiveClickableMenuUnscaled && !Game1.IsActiveClickableMenuNativeScaled)
                                        {
                                            Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                        }
                                        events.RenderedActiveMenu.RaiseEmpty();
                                    }
                                    catch (Exception ex)
                                    {
                                        this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                        Game1.activeClickableMenu.exitThisMenu();
                                    }
                                }
                                else if (Game1.farmEvent != null)
                                {
                                    Game1.farmEvent.drawAboveEverything(Game1.spriteBatch);
                                }
                                if (Game1.HostPaused)
                                {
                                    string s = Game1.content.LoadString(@"Strings\StringsFromCSFiles:DayTimeMoneyBox.cs.10378");
                                    SpriteText.drawStringWithScrollBackground(Game1.spriteBatch, s, 0x60, 0x20, "", 1f, -1, 0.088f);
                                }
                                _spriteBatchEnd.Invoke();
                                drawOverlays.Invoke(Game1.spriteBatch);
                                renderScreenBuffer.Invoke(BlendState.Opaque);
                                if (_drawHUD.GetValue())
                                {
                                    DrawDayTimeMoneyBox.Invoke();
                                    Game1.SetSpriteBatchBeginNextID("A-C");
                                    SpriteBatchBegin.Invoke(1f);
                                    events.RenderingHud.RaiseEmpty();
                                    DrawHUD.Invoke();
                                    events.RenderedHud.RaiseEmpty();
                                    if (((Game1.currentLocation != null) && !(Game1.activeClickableMenu is GameMenu)) && !(Game1.activeClickableMenu is QuestLog))
                                    {
                                        Game1.currentLocation.drawAboveAlwaysFrontLayerText(Game1.spriteBatch);
                                    }
                                    DrawAfterMap.Invoke();
                                    _spriteBatchEnd.Invoke();
                                    if (Game1.tutorialManager != null)
                                    {
                                        Game1.SetSpriteBatchBeginNextID("A-D");
                                        SpriteBatchBegin.Invoke(Game1.options.zoomLevel);
                                        Game1.tutorialManager.draw(Game1.spriteBatch);
                                        _spriteBatchEnd.Invoke();
                                    }
                                    DrawToolbar.Invoke();
                                    DrawVirtualJoypad.Invoke();
                                }
                                DrawFadeToBlackFullScreenRect.Invoke();
                                Game1.SetSpriteBatchBeginNextID("A-E");
                                SpriteBatchBegin.Invoke(1f);
                                DrawChatBox.Invoke();
                                _spriteBatchEnd.Invoke();
                                if (_drawActiveClickableMenu.GetValue())
                                {
                                    DrawDialogueBoxForPinchZoom.Invoke();
                                    DrawUnscaledActiveClickableMenuForPinchZoom.Invoke();
                                    DrawNativeScaledActiveClickableMenuForPinchZoom.Invoke();
                                }
                                if ((_drawHUD.GetValue() && (Game1.hudMessages.Count > 0)) && (!Game1.eventUp || Game1.isFestival()))
                                {
                                    Game1.SetSpriteBatchBeginNextID("A-F");
                                    SpriteBatchBegin.Invoke(Game1.NativeZoomLevel);
                                    DrawHUDMessages.Invoke();
                                    _spriteBatchEnd.Invoke();
                                }
                                if (((Game1.CurrentEvent != null) && Game1.CurrentEvent.skippable) && ((Game1.activeClickableMenu == null) || ((Game1.activeClickableMenu != null) && !(Game1.activeClickableMenu is MenuWithInventory))))
                                {
                                    Game1.SetSpriteBatchBeginNextID("A-G");
                                    SpriteBatchBegin.Invoke(Game1.NativeZoomLevel);
                                    Game1.CurrentEvent.DrawSkipButton(Game1.spriteBatch);
                                    _spriteBatchEnd.Invoke();
                                }
                                DrawTutorialUI.Invoke();
                                events.Rendered.RaiseEmpty();
                            }
                        }
                    }
                }
            }
        }
    }
}
