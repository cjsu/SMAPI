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
#if !SMAPI_3_0_STRICT
using Microsoft.Xna.Framework.Input;
#endif
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
using StardewValley.Minigames;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Dimensions;
using xTile.Layers;
using SObject = StardewValley.Object;

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

        /// <summary>Whether the game is saving and SMAPI has already raised <see cref="IGameLoopEvents.Saving"/>.</summary>
        private bool IsBetweenSaveEvents;

        /// <summary>Whether the game is creating the save file and SMAPI has already raised <see cref="IGameLoopEvents.SaveCreating"/>.</summary>
        private bool IsBetweenCreateEvents;

        /// <summary>A callback to invoke after the content language changes.</summary>
        private readonly Action OnLocaleChanged;

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
        /// <param name="onLocaleChanged">A callback to invoke after the content language changes.</param>
        /// <param name="onGameInitialised">A callback to invoke after the game finishes initialising.</param>
        /// <param name="onGameExiting">A callback to invoke when the game exits.</param>
        public SGame(IMonitor monitor, IMonitor monitorForGame, Reflector reflection, EventManager eventManager, JsonHelper jsonHelper, ModRegistry modRegistry, DeprecationManager deprecationManager, Action onLocaleChanged, Action onGameInitialised, Action onGameExiting)
        {
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
            this.OnLocaleChanged = onLocaleChanged;
            this.OnGameInitialised = onGameInitialised;
            this.OnGameExiting = onGameExiting;
            Game1.input = new SInputState();
            Game1.multiplayer = new SMultiplayer(monitor, eventManager, jsonHelper, modRegistry, reflection, this.OnModMessageReceived);
            Game1.hooks = new SModHooks(this.OnNewDayAfterFade);

            // init observables
            //this.Reflection.GetField<IList<GameLocation>>(typeof(Game1), "locations").SetValue(new ObservableCollection<GameLocation>());
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
            Game1.multiplayer.Disconnect();
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
            {
                this.Events.ReturnedToTitle.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                this.Events.Legacy_AfterReturnToTitle.Raise();
#endif
            }
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
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, SGame.ConstructorHack.Monitor, SGame.ConstructorHack.Reflection, SGame.ConstructorHack.JsonHelper);
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

                //PROBLEM CHILD
                
                // Run async tasks synchronously to avoid issues due to mod events triggering
                // concurrently with game code.
                //bool saveParsed = false;
                //if (Game1.currentLoader != null)
                //{
                //    this.Monitor.Log("Game loader synchronising...", LogLevel.Trace);
                //    while (Game1.currentLoader?.MoveNext() == true)
                //    {
                //        this.Monitor.Log($"load step: {Game1.currentLoader.Current}, SaveGame.loaded: {(SaveGame.loaded != null ? "set" : "null")}, saveParsed: {saveParsed}, gameMode: {Game1.gameMode}");
                //        // raise load stage changed
                //        switch (Game1.currentLoader.Current)
                //        {
                //            case 20:
                //                if (!saveParsed && SaveGame.loaded != null)
                //                {
                //                    saveParsed = true;
                //                    this.OnLoadStageChanged(LoadStage.SaveParsed);
                //                }
                //                break;

                //            case 36:
                //                this.OnLoadStageChanged(LoadStage.SaveLoadedBasicInfo);
                //                break;

                //            case 50:
                //                this.OnLoadStageChanged(LoadStage.SaveLoadedLocations);
                //                break;

                //            default:
                //                if (Game1.gameMode == Game1.playingGameMode)
                //                    this.OnLoadStageChanged(LoadStage.Preloaded);
                //                break;
                //        }
                //    }

                //    Game1.currentLoader = null;
                //    this.Monitor.Log("Game loader done.", LogLevel.Trace);

                //}
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
                if (_newDayTask != null || Game1.gameMode == Game1.loadingMode)
                {
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    SGame.TicksElapsed++;
                    base.Update(gameTime);
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_UnvalidatedUpdateTick.Raise();
#endif
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
#if !SMAPI_3_0_STRICT
                SInputState previousInputState = this.Input.Clone();
#endif
                SInputState inputState = this.Input;
                if (this.IsActive)
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
#if !SMAPI_3_0_STRICT
                        events.Legacy_BeforeCreateSave.Raise();
#endif
                    }

                    // raise before-save
                    if (Context.IsWorldReady && !this.IsBetweenSaveEvents)
                    {
                        this.IsBetweenSaveEvents = true;
                        this.Monitor.Log("Context: before save.", LogLevel.Trace);
                        events.Saving.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                        events.Legacy_BeforeSave.Raise();
#endif
                    }

                    // suppress non-save events
                    events.UnvalidatedUpdateTicking.RaiseEmpty();
                    SGame.TicksElapsed++;
                    base.Update(gameTime);
                    events.UnvalidatedUpdateTicked.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_UnvalidatedUpdateTick.Raise();
#endif
                    return;
                }
                if (this.IsBetweenCreateEvents)
                {
                    // raise after-create
                    this.IsBetweenCreateEvents = false;
                    this.Monitor.Log($"Context: after save creation, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.", LogLevel.Trace);
                    this.OnLoadStageChanged(LoadStage.CreatedSaveFile);
                    events.SaveCreated.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_AfterCreateSave.Raise();
#endif
                }
                if (this.IsBetweenSaveEvents)
                {
                    // raise after-save
                    this.IsBetweenSaveEvents = false;
                    this.Monitor.Log($"Context: after save, starting {Game1.currentSeason} {Game1.dayOfMonth} Y{Game1.year}.", LogLevel.Trace);
                    events.Saved.RaiseEmpty();
                    events.DayStarted.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_AfterSave.Raise();
                    events.Legacy_AfterDayStarted.Raise();
#endif
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
                    var was = this.Watchers.LocaleWatcher.PreviousValue;
                    var now = this.Watchers.LocaleWatcher.CurrentValue;

                    this.Monitor.Log($"Context: locale set to {now}.", LogLevel.Trace);

                    this.OnLocaleChanged();
#if !SMAPI_3_0_STRICT
                    events.Legacy_LocaleChanged.Raise(new EventArgsValueChanged<string>(was.ToString(), now.ToString()));
#endif

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
#if !SMAPI_3_0_STRICT
                    events.Legacy_AfterLoad.Raise();
                    events.Legacy_AfterDayStarted.Raise();
#endif
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
#if !SMAPI_3_0_STRICT
                    events.Legacy_Resize.Raise();
#endif
                    this.Watchers.WindowSizeWatcher.Reset();
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

#if !SMAPI_3_0_STRICT
                                // legacy events
                                events.Legacy_ButtonPressed.Raise(new EventArgsInput(button, cursor, inputState.SuppressButtons));
                                if (button.TryGetKeyboard(out Keys key))
                                {
                                    if (key != Keys.None)
                                        events.Legacy_KeyPressed.Raise(new EventArgsKeyPressed(key));
                                }
                                else if (button.TryGetController(out Buttons controllerButton))
                                {
                                    if (controllerButton == Buttons.LeftTrigger || controllerButton == Buttons.RightTrigger)
                                        events.Legacy_ControllerTriggerPressed.Raise(new EventArgsControllerTriggerPressed(PlayerIndex.One, controllerButton, controllerButton == Buttons.LeftTrigger ? inputState.RealController.Triggers.Left : inputState.RealController.Triggers.Right));
                                    else
                                        events.Legacy_ControllerButtonPressed.Raise(new EventArgsControllerButtonPressed(PlayerIndex.One, controllerButton));
                                }
#endif
                            }
                            else if (status == InputStatus.Released)
                            {
                                if (this.Monitor.IsVerbose)
                                    this.Monitor.Log($"Events: button {button} released.", LogLevel.Trace);

                                events.ButtonReleased.Raise(new ButtonReleasedEventArgs(button, cursor, inputState));

#if !SMAPI_3_0_STRICT
                                // legacy events
                                events.Legacy_ButtonReleased.Raise(new EventArgsInput(button, cursor, inputState.SuppressButtons));
                                if (button.TryGetKeyboard(out Keys key))
                                {
                                    if (key != Keys.None)
                                        events.Legacy_KeyReleased.Raise(new EventArgsKeyPressed(key));
                                }
                                else if (button.TryGetController(out Buttons controllerButton))
                                {
                                    if (controllerButton == Buttons.LeftTrigger || controllerButton == Buttons.RightTrigger)
                                        events.Legacy_ControllerTriggerReleased.Raise(new EventArgsControllerTriggerReleased(PlayerIndex.One, controllerButton, controllerButton == Buttons.LeftTrigger ? inputState.RealController.Triggers.Left : inputState.RealController.Triggers.Right));
                                    else
                                        events.Legacy_ControllerButtonReleased.Raise(new EventArgsControllerButtonReleased(PlayerIndex.One, controllerButton));
                                }
#endif
                            }
                        }

#if !SMAPI_3_0_STRICT
                        // raise legacy state-changed events
                        if (inputState.RealKeyboard != previousInputState.RealKeyboard)
                            events.Legacy_KeyboardChanged.Raise(new EventArgsKeyboardStateChanged(previousInputState.RealKeyboard, inputState.RealKeyboard));
                        if (inputState.RealMouse != previousInputState.RealMouse)
                            events.Legacy_MouseChanged.Raise(new EventArgsMouseStateChanged(previousInputState.RealMouse, inputState.RealMouse, new Point((int)previousInputState.CursorPosition.ScreenPixels.X, (int)previousInputState.CursorPosition.ScreenPixels.Y), new Point((int)inputState.CursorPosition.ScreenPixels.X, (int)inputState.CursorPosition.ScreenPixels.Y)));
#endif
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
#if !SMAPI_3_0_STRICT
                    if (now != null)
                        events.Legacy_MenuChanged.Raise(new EventArgsClickableMenuChanged(was, now));
                    else
                        events.Legacy_MenuClosed.Raise(new EventArgsClickableMenuClosed(was));
#endif
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
#if !SMAPI_3_0_STRICT
                            events.Legacy_LocationsChanged.Raise(new EventArgsLocationsChanged(added, removed));
#endif
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
#if !SMAPI_3_0_STRICT
                                    events.Legacy_BuildingsChanged.Raise(new EventArgsLocationBuildingsChanged(location, added, removed));
#endif
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
#if !SMAPI_3_0_STRICT
                                    events.Legacy_ObjectsChanged.Raise(new EventArgsLocationObjectsChanged(location, added, removed));
#endif
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
#if !SMAPI_3_0_STRICT
                        events.Legacy_TimeOfDayChanged.Raise(new EventArgsIntChanged(was, now));
#endif
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
#if !SMAPI_3_0_STRICT
                            events.Legacy_PlayerWarped.Raise(new EventArgsPlayerWarped(oldLocation, newLocation));
#endif
                        }

                        // raise player leveled up a skill
                        foreach (KeyValuePair<SkillType, IValueWatcher<int>> pair in playerTracker.GetChangedSkills())
                        {
                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log($"Events: player skill '{pair.Key}' changed from {pair.Value.PreviousValue} to {pair.Value.CurrentValue}.", LogLevel.Trace);

                            events.LevelChanged.Raise(new LevelChangedEventArgs(playerTracker.Player, pair.Key, pair.Value.PreviousValue, pair.Value.CurrentValue));
#if !SMAPI_3_0_STRICT
                            events.Legacy_LeveledUp.Raise(new EventArgsLevelUp((EventArgsLevelUp.LevelType)pair.Key, pair.Value.CurrentValue));
#endif
                        }

                        // raise player inventory changed
                        ItemStackChange[] changedItems = playerTracker.GetInventoryChanges().ToArray();
                        if (changedItems.Any())
                        {
                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log("Events: player inventory changed.", LogLevel.Trace);
                            events.InventoryChanged.Raise(new InventoryChangedEventArgs(playerTracker.Player, changedItems));
#if !SMAPI_3_0_STRICT
                            events.Legacy_InventoryChanged.Raise(new EventArgsInventoryChanged(Game1.player.Items, changedItems));
#endif
                        }

                        // raise mine level changed
                        if (playerTracker.TryGetNewMineLevel(out int mineLevel))
                        {
                            if (this.Monitor.IsVerbose)
                                this.Monitor.Log($"Context: mine level changed to {mineLevel}.", LogLevel.Trace);
#if !SMAPI_3_0_STRICT
                            events.Legacy_MineLevelChanged.Raise(new EventArgsMineLevelChanged(playerTracker.MineLevelWatcher.PreviousValue, mineLevel));
#endif
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
                    events.GameLaunched.Raise(new GameLaunchedEventArgs());

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

                /*********
                ** Update events
                *********/
#if !SMAPI_3_0_STRICT
                events.Legacy_UnvalidatedUpdateTick.Raise();
                if (isFirstTick)
                    events.Legacy_FirstUpdateTick.Raise();
                events.Legacy_UpdateTick.Raise();
                if (SGame.TicksElapsed % 2 == 0)
                    events.Legacy_SecondUpdateTick.Raise();
                if (SGame.TicksElapsed % 4 == 0)
                    events.Legacy_FourthUpdateTick.Raise();
                if (SGame.TicksElapsed % 8 == 0)
                    events.Legacy_EighthUpdateTick.Raise();
                if (SGame.TicksElapsed % 15 == 0)
                    events.Legacy_QuarterSecondTick.Raise();
                if (SGame.TicksElapsed % 30 == 0)
                    events.Legacy_HalfSecondTick.Raise();
                if (SGame.TicksElapsed % 60 == 0)
                    events.Legacy_OneSecondTick.Raise();
#endif

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

        /****
        ** Methods
        ****/
#if !SMAPI_3_0_STRICT
        /// <summary>Raise the <see cref="GraphicsEvents.OnPostRenderEvent"/> if there are any listeners.</summary>
        /// <param name="needsNewBatch">Whether to create a new sprite batch.</param>
        private void RaisePostRender(bool needsNewBatch = false)
        {
            if (this.Events.Legacy_OnPostRenderEvent.HasListeners())
            {
                if (needsNewBatch)
                    Game1.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
                this.Events.Legacy_OnPostRenderEvent.Raise();
                if (needsNewBatch)
                    Game1.spriteBatch.End();
            }
        }
#endif

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
            if (skipNextDrawCall)
            {
                skipNextDrawCall = false;
            }
            else
            {
                IReflectedField<bool> _drawActiveClickableMenu = this.Reflection.GetField<bool>(this, "_drawActiveClickableMenu");
                IReflectedField<bool> _drawHUD = this.Reflection.GetField<bool>(this, "_drawHUD");
                IReflectedField<Color> bgColor = this.Reflection.GetField<Color>(this, "bgColor");
                IReflectedField<List<Farmer>> _farmerShadows = this.Reflection.GetField<List<Farmer>>(this, "_farmerShadows");
                IReflectedField<StringBuilder> _debugStringBuilder = this.Reflection.GetField<StringBuilder>(typeof(Game1), "_debugStringBuilder");
                IReflectedField<BlendState> lightingBlend = this.Reflection.GetField<BlendState>(this, "lightingBlend");

                IReflectedMethod SpriteBatchBegin = this.Reflection.GetMethod(this, "SpriteBatchBegin", new Type[] { typeof(float) });
                IReflectedMethod _spriteBatchBegin = this.Reflection.GetMethod(this, "_spriteBatchBegin", new Type[] { typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState), typeof(Effect), typeof(Matrix)});
                IReflectedMethod _spriteBatchEnd = this.Reflection.GetMethod(this, "_spriteBatchEnd", new Type[] { });
                IReflectedMethod DrawLoadingDotDotDot = this.Reflection.GetMethod(this, "DrawLoadingDotDotDot", new Type[] { typeof(GameTime) });
                IReflectedMethod CheckToReloadGameLocationAfterDrawFail = this.Reflection.GetMethod(this, "CheckToReloadGameLocationAfterDrawFail", new Type[] { typeof(string), typeof(Exception) });
                IReflectedMethod DrawTapToMoveTarget = this.Reflection.GetMethod(this, "DrawTapToMoveTarget", new Type[] { });
                IReflectedMethod DrawDayTimeMoneyBox = this.Reflection.GetMethod(this, "DrawDayTimeMoneyBox", new Type[] { });
                IReflectedMethod DrawAfterMap = this.Reflection.GetMethod(this, "DrawAfterMap", new Type[] { });
                IReflectedMethod DrawToolbar = this.Reflection.GetMethod(this, "DrawToolbar", new Type[] { });
                IReflectedMethod DrawVirtualJoypad = this.Reflection.GetMethod(this, "DrawVirtualJoypad", new Type[] { });
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
                if (_newDayTask != null)
                {
                    base.GraphicsDevice.Clear(bgColor.GetValue());
                    return;
                }
                if (options.zoomLevel != 1f)
                {
                    base.GraphicsDevice.SetRenderTarget(screen);
                }
                if (IsSaving)
                {
                    base.GraphicsDevice.Clear(bgColor.GetValue());
                    renderScreenBuffer(BlendState.Opaque);
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
#if !SMAPI_3_0_STRICT
                                events.Legacy_OnPreRenderGuiEvent.Raise();
#endif
                                Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                events.RenderedActiveMenu.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                                events.Legacy_OnPostRenderGuiEvent.Raise();
#endif
                            }
                            catch (Exception ex)
                            {
                                this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during save. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                Game1.activeClickableMenu.exitThisMenu();
                            }
#if !SMAPI_3_0_STRICT
                            events.Legacy_OnPostRenderEvent.Raise();
#endif
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
#if !SMAPI_3_0_STRICT
                                events.Legacy_OnPreRenderGuiEvent.Raise();
#endif
                                Game1.activeClickableMenu.draw(Game1.spriteBatch);
                                events.RenderedActiveMenu.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                                events.Legacy_OnPostRenderGuiEvent.Raise();
#endif
                            }
                            catch (Exception ex)
                            {
                                this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during save. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                                Game1.activeClickableMenu.exitThisMenu();
                            }
                            events.Rendered.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                            events.Legacy_OnPostRenderEvent.Raise();
#endif

                            _spriteBatchEnd.Invoke();
                            RestoreViewportAndZoom();
                        }
                    }
                    if (overlayMenu != null)
                    {
                        BackupViewportAndZoom();
                        SetSpriteBatchBeginNextID("B");
                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        overlayMenu.draw(spriteBatch);
                        _spriteBatchEnd.Invoke();
                        RestoreViewportAndZoom();
                    }
                    return;
                }
                base.GraphicsDevice.Clear(bgColor.GetValue());
                if (activeClickableMenu != null && options.showMenuBackground && activeClickableMenu.showWithoutTransparencyIfOptionIsSet())
                {
                    Matrix value = Matrix.CreateScale(1f);
                    SetSpriteBatchBeginNextID("C");
                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, value);
                    events.Rendering.RaiseEmpty();
                    try
                    {
                        Game1.activeClickableMenu.drawBackground(Game1.spriteBatch);
                        events.RenderingActiveMenu.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                        events.Legacy_OnPreRenderGuiEvent.Raise();
#endif
                        Game1.activeClickableMenu.drawBackground(Game1.spriteBatch);
                        Game1.activeClickableMenu.draw(Game1.spriteBatch);
                        events.RenderedActiveMenu.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                        events.Legacy_OnPostRenderGuiEvent.Raise();
#endif
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                        Game1.activeClickableMenu.exitThisMenu();
                    }
                    events.Rendered.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_OnPostRenderEvent.Raise();
#endif

                    _spriteBatchEnd.Invoke();
                    drawOverlays(spriteBatch);
                    renderScreenBuffer(BlendState.AlphaBlend);
                    if (overlayMenu != null)
                    {
                        SetSpriteBatchBeginNextID("D");
                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        overlayMenu.draw(spriteBatch);
                        _spriteBatchEnd.Invoke();
                    }
                    return;
                }
                if (emergencyLoading)
                {
                    if (!SeenConcernedApeLogo)
                    {
                        SetSpriteBatchBeginNextID("E");
                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        if (logoFadeTimer < 5000)
                        {
                            spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
                        }
                        if (logoFadeTimer > 4500)
                        {
                            float scale = System.Math.Min(1f, (float)(logoFadeTimer - 4500) / 500f);
                            spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * scale);
                        }
                        spriteBatch.Draw(titleButtonsTexture, new Vector2(viewport.Width / 2, viewport.Height / 2 - 90), new Microsoft.Xna.Framework.Rectangle(171 + ((logoFadeTimer / 100 % 2 == 0) ? 111 : 0), 311, 111, 60), Color.White * ((logoFadeTimer < 500) ? ((float)logoFadeTimer / 500f) : ((logoFadeTimer > 4500) ? (1f - (float)(logoFadeTimer - 4500) / 500f) : 1f)), 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.2f);
                        spriteBatch.Draw(titleButtonsTexture, new Vector2(viewport.Width / 2 - 261, viewport.Height / 2 - 102), new Microsoft.Xna.Framework.Rectangle((logoFadeTimer / 100 % 2 == 0) ? 85 : 0, 306, 85, 69), Color.White * ((logoFadeTimer < 500) ? ((float)logoFadeTimer / 500f) : ((logoFadeTimer > 4500) ? (1f - (float)(logoFadeTimer - 4500) / 500f) : 1f)), 0f, Vector2.Zero, 3f, SpriteEffects.None, 0.2f);
                        _spriteBatchEnd.Invoke();
                    }
                    logoFadeTimer -= gameTime.ElapsedGameTime.Milliseconds;
                }
                if (gameMode == 11)
                {
                    SetSpriteBatchBeginNextID("F");
                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                    events.Rendering.RaiseEmpty();
                    spriteBatch.DrawString(dialogueFont, content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3685"), new Vector2(16f, 16f), Color.HotPink);
                    spriteBatch.DrawString(dialogueFont, content.LoadString("Strings\\StringsFromCSFiles:Game1.cs.3686"), new Vector2(16f, 32f), new Color(0, 255, 0));
                    spriteBatch.DrawString(dialogueFont, parseText(errorMessage, dialogueFont, graphics.GraphicsDevice.Viewport.Width), new Vector2(16f, 48f), Color.White);
                    events.Rendered.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_OnPostRenderEvent.Raise();
#endif

                    _spriteBatchEnd.Invoke();
                    return;
                }
                if (currentMinigame != null)
                {
                    currentMinigame.draw(spriteBatch);
                    if (globalFade && !menuUp && (!nameSelectUp || messagePause))
                    {
                        SetSpriteBatchBeginNextID("G");
                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        spriteBatch.Draw(fadeToBlackRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Black * ((gameMode == 0) ? (1f - fadeToBlackAlpha) : fadeToBlackAlpha));
                        _spriteBatchEnd.Invoke();
                    }
                    drawOverlays(spriteBatch);
                    renderScreenBuffer(BlendState.AlphaBlend);
                    if ((currentMinigame is FishingGame || currentMinigame is FantasyBoardGame) && activeClickableMenu != null)
                    {
                        SetSpriteBatchBeginNextID("A-A");
                        SpriteBatchBegin.Invoke(IsActiveClickableMenuNativeScaled ? NativeZoomLevel : 1f);
                        activeClickableMenu.draw(spriteBatch);
                        _spriteBatchEnd.Invoke();
                        drawOverlays(spriteBatch);
                    }
                    return;
                }
                if (showingEndOfNightStuff)
                {
                    renderScreenBuffer(BlendState.Opaque);
                    BackupViewportAndZoom(divideByZoom: true);
                    SetSpriteBatchBeginNextID("A-B");
                    SpriteBatchBegin.Invoke(IsActiveClickableMenuNativeScaled ? NativeZoomLevel : 1f);
                    events.Rendering.RaiseEmpty();
                    if (activeClickableMenu != null)
                    {
                        try
                        {
                            events.RenderingActiveMenu.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                            events.Legacy_OnPreRenderGuiEvent.Raise();
#endif
                            Game1.activeClickableMenu.draw(Game1.spriteBatch);
                            events.RenderedActiveMenu.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                            events.Legacy_OnPostRenderGuiEvent.Raise();
#endif
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"The {Game1.activeClickableMenu.GetType().FullName} menu crashed while drawing itself during end-of-night-stuff. SMAPI will force it to exit to avoid crashing the game.\n{ex.GetLogSummary()}", LogLevel.Error);
                            Game1.activeClickableMenu.exitThisMenu();
                        }
                    }

                    events.Rendered.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_OnPostRenderEvent.Raise();
#endif

                    _spriteBatchEnd.Invoke();
                    drawOverlays(spriteBatch);
                    RestoreViewportAndZoom();
                    return;
                }
                if (gameMode == 6 || (gameMode == 3 && currentLocation == null))
                {
                    events.Rendering.RaiseEmpty();
                    DrawLoadingDotDotDot.Invoke(gameTime);
                    events.Rendered.RaiseEmpty();

#if !SMAPI_3_0_STRICT
                    events.Legacy_OnPostRenderEvent.Raise();
#endif

                    drawOverlays(spriteBatch);
                    renderScreenBuffer(BlendState.AlphaBlend);
                    if (overlayMenu != null)
                    {
                        SetSpriteBatchBeginNextID("H");
                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                        overlayMenu.draw(spriteBatch);
                        _spriteBatchEnd.Invoke();
                    }
                    base.Draw(gameTime);
                    return;
                }
                if (gameMode == 0)
                {
                    SetSpriteBatchBeginNextID("I");
                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                    events.Rendering.RaiseEmpty();
                }
                else if (!drawGame)
                {
                    SetSpriteBatchBeginNextID("J");
                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, null);
                    events.Rendering.RaiseEmpty();
                }
                else if (drawGame)
                {
                    if (drawLighting)
                    {
                        base.GraphicsDevice.SetRenderTarget(lightmap);
                        base.GraphicsDevice.Clear(Color.White * 0f);
                        SetSpriteBatchBeginNextID("K");
                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null, null, null);
                        events.Rendering.RaiseEmpty();
                        spriteBatch.Draw(staminaRect, lightmap.Bounds, currentLocation.Name.StartsWith("UndergroundMine") ? mine.getLightingColor(gameTime) : ((!ambientLight.Equals(Color.White) && (!RainManager.Instance.isRaining || !currentLocation.isOutdoors)) ? ambientLight : outdoorLight));
                        for (int i = 0; i < currentLightSources.Count; i++)
                        {
                            if (Utility.isOnScreen(currentLightSources.ElementAt(i).position, (int)((float)currentLightSources.ElementAt(i).radius * 64f * 4f)))
                            {
                                spriteBatch.Draw(currentLightSources.ElementAt(i).lightTexture, GlobalToLocal(viewport, currentLightSources.ElementAt(i).position) / (options.lightingQuality / 2), currentLightSources.ElementAt(i).lightTexture.Bounds, currentLightSources.ElementAt(i).color, 0f, new Vector2(currentLightSources.ElementAt(i).lightTexture.Bounds.Center.X, currentLightSources.ElementAt(i).lightTexture.Bounds.Center.Y), (float)currentLightSources.ElementAt(i).radius / (float)(options.lightingQuality / 2), SpriteEffects.None, 0.9f);
                            }
                        }
                        _spriteBatchEnd.Invoke();
                        base.GraphicsDevice.SetRenderTarget((options.zoomLevel == 1f) ? null : screen);
                    }
                    if (bloomDay && bloom != null)
                    {
                        bloom.BeginDraw();
                    }
                    base.GraphicsDevice.Clear(bgColor.GetValue());
                    SetSpriteBatchBeginNextID("L");
                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                    events.Rendering.RaiseEmpty();
                    events.RenderingWorld.RaiseEmpty();
                    if (background != null)
                    {
                        background.draw(spriteBatch);
                    }
                    mapDisplayDevice.BeginScene(spriteBatch);
                    try
                    {
                        currentLocation.Map.GetLayer("Back").Draw(mapDisplayDevice, viewport, Location.Origin, wrapAround: false, 4);
                    }
                    catch (KeyNotFoundException exception)
                    {
                        CheckToReloadGameLocationAfterDrawFail.Invoke("Back", exception);
                    }
                    currentLocation.drawWater(spriteBatch);
                    _farmerShadows.GetValue().Clear();
                    if (currentLocation.currentEvent != null && !currentLocation.currentEvent.isFestival && currentLocation.currentEvent.farmerActors.Count > 0)
                    {
                        foreach (Farmer farmerActor in currentLocation.currentEvent.farmerActors)
                        {
                            if ((farmerActor.IsLocalPlayer && displayFarmer) || !farmerActor.hidden)
                            {
                                _farmerShadows.GetValue().Add(farmerActor);
                            }
                        }
                    }
                    else
                    {
                        foreach (Farmer farmer in currentLocation.farmers)
                        {
                            if ((farmer.IsLocalPlayer && displayFarmer) || !farmer.hidden)
                            {
                                _farmerShadows.GetValue().Add(farmer);
                            }
                        }
                    }
                    if (!currentLocation.shouldHideCharacters())
                    {
                        if (CurrentEvent == null)
                        {
                            foreach (NPC character in currentLocation.characters)
                            {
                                if (!character.swimming && !character.HideShadow && !character.IsInvisible && !currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(character.getTileLocation()))
                                {
                                    spriteBatch.Draw(shadowTexture, GlobalToLocal(viewport, character.Position + new Vector2((float)(character.Sprite.SpriteWidth * 4) / 2f, character.GetBoundingBox().Height + ((!character.IsMonster) ? 12 : 0))), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), (4f + (float)character.yJumpOffset / 40f) * (float)character.scale, SpriteEffects.None, System.Math.Max(0f, (float)character.getStandingY() / 10000f) - 1E-06f);
                                }
                            }
                        }
                        else
                        {
                            foreach (NPC actor in CurrentEvent.actors)
                            {
                                if (!actor.swimming && !actor.HideShadow && !currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(actor.getTileLocation()))
                                {
                                    spriteBatch.Draw(shadowTexture, GlobalToLocal(viewport, actor.Position + new Vector2((float)(actor.Sprite.SpriteWidth * 4) / 2f, actor.GetBoundingBox().Height + ((!actor.IsMonster) ? ((actor.Sprite.SpriteHeight <= 16) ? (-4) : 12) : 0))), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), (4f + (float)actor.yJumpOffset / 40f) * (float)actor.scale, SpriteEffects.None, System.Math.Max(0f, (float)actor.getStandingY() / 10000f) - 1E-06f);
                                }
                            }
                        }
                        foreach (Farmer farmerShadow in _farmerShadows.GetValue())
                        {
                            if (!farmerShadow.swimming && !farmerShadow.isRidingHorse() && (currentLocation == null || !currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmerShadow.getTileLocation())))
                            {
                                spriteBatch.Draw(shadowTexture, GlobalToLocal(farmerShadow.Position + new Vector2(32f, 24f)), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), 4f - (((farmerShadow.running || farmerShadow.UsingTool) && farmerShadow.FarmerSprite.currentAnimationIndex > 1) ? ((float)System.Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmerShadow.FarmerSprite.CurrentFrame]) * 0.5f) : 0f), SpriteEffects.None, 0f);
                            }
                        }
                    }
                    try
                    {
                        currentLocation.Map.GetLayer("Buildings").Draw(mapDisplayDevice, viewport, Location.Origin, wrapAround: false, 4);
                    }
                    catch (KeyNotFoundException exception2)
                    {
                        CheckToReloadGameLocationAfterDrawFail.Invoke("Buildings", exception2);
                    }
                    mapDisplayDevice.EndScene();
                    if (currentLocation.tapToMove.targetNPC != null)
                    {
                        spriteBatch.Draw(mouseCursors, GlobalToLocal(viewport, currentLocation.tapToMove.targetNPC.Position + new Vector2((float)(currentLocation.tapToMove.targetNPC.Sprite.SpriteWidth * 4) / 2f - 32f, currentLocation.tapToMove.targetNPC.GetBoundingBox().Height + ((!currentLocation.tapToMove.targetNPC.IsMonster) ? 12 : 0) - 32)), new Microsoft.Xna.Framework.Rectangle(194, 388, 16, 16), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.58f);
                    }
                    _spriteBatchEnd.Invoke();
                    SetSpriteBatchBeginNextID("M");
                    _spriteBatchBegin.Invoke(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                    if (!currentLocation.shouldHideCharacters())
                    {
                        if (CurrentEvent == null)
                        {
                            foreach (NPC character2 in currentLocation.characters)
                            {
                                if (!character2.swimming && !character2.HideShadow && currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(character2.getTileLocation()))
                                {
                                    spriteBatch.Draw(shadowTexture, GlobalToLocal(viewport, character2.Position + new Vector2((float)(character2.Sprite.SpriteWidth * 4) / 2f, character2.GetBoundingBox().Height + ((!character2.IsMonster) ? 12 : 0))), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), (4f + (float)character2.yJumpOffset / 40f) * (float)character2.scale, SpriteEffects.None, System.Math.Max(0f, (float)character2.getStandingY() / 10000f) - 1E-06f);
                                }
                            }
                        }
                        else
                        {
                            foreach (NPC actor2 in CurrentEvent.actors)
                            {
                                if (!actor2.swimming && !actor2.HideShadow && currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(actor2.getTileLocation()))
                                {
                                    spriteBatch.Draw(shadowTexture, GlobalToLocal(viewport, actor2.Position + new Vector2((float)(actor2.Sprite.SpriteWidth * 4) / 2f, actor2.GetBoundingBox().Height + ((!actor2.IsMonster) ? 12 : 0))), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), (4f + (float)actor2.yJumpOffset / 40f) * (float)actor2.scale, SpriteEffects.None, System.Math.Max(0f, (float)actor2.getStandingY() / 10000f) - 1E-06f);
                                }
                            }
                        }
                        foreach (Farmer farmerShadow2 in _farmerShadows.GetValue())
                        {
                            if (!farmerShadow2.swimming && !farmerShadow2.isRidingHorse() && currentLocation != null && currentLocation.shouldShadowBeDrawnAboveBuildingsLayer(farmerShadow2.getTileLocation()))
                            {
                                spriteBatch.Draw(shadowTexture, GlobalToLocal(farmerShadow2.Position + new Vector2(32f, 24f)), shadowTexture.Bounds, Color.White, 0f, new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y), 4f - (((farmerShadow2.running || farmerShadow2.UsingTool) && farmerShadow2.FarmerSprite.currentAnimationIndex > 1) ? ((float)System.Math.Abs(FarmerRenderer.featureYOffsetPerFrame[farmerShadow2.FarmerSprite.CurrentFrame]) * 0.5f) : 0f), SpriteEffects.None, 0f);
                            }
                        }
                    }
                    if ((eventUp || killScreen) && !killScreen && currentLocation.currentEvent != null)
                    {
                        currentLocation.currentEvent.draw(spriteBatch);
                    }
                    if (player.currentUpgrade != null && player.currentUpgrade.daysLeftTillUpgradeDone <= 3 && currentLocation.Name.Equals("Farm"))
                    {
                        spriteBatch.Draw(player.currentUpgrade.workerTexture, GlobalToLocal(viewport, player.currentUpgrade.positionOfCarpenter), player.currentUpgrade.getSourceRectangle(), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, (player.currentUpgrade.positionOfCarpenter.Y + 48f) / 10000f);
                    }
                    currentLocation.draw(spriteBatch);
                    if (player.ActiveObject == null && (player.UsingTool || pickingTool) && player.CurrentTool != null && (!player.CurrentTool.Name.Equals("Seeds") || pickingTool))
                    {
                        drawTool(player);
                    }
                    if (currentLocation.Name.Equals("Farm"))
                    {
                        drawFarmBuildings();
                    }
                    if (tvStation >= 0)
                    {
                        spriteBatch.Draw(tvStationTexture, GlobalToLocal(viewport, new Vector2(400f, 160f)), new Microsoft.Xna.Framework.Rectangle(tvStation * 24, 0, 24, 15), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1E-08f);
                    }
                    if (panMode)
                    {
                        spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((int)System.Math.Floor((double)(getOldMouseX() + viewport.X) / 64.0) * 64 - viewport.X, (int)System.Math.Floor((double)(getOldMouseY() + viewport.Y) / 64.0) * 64 - viewport.Y, 64, 64), Color.Lime * 0.75f);
                        foreach (Warp warp in currentLocation.warps)
                        {
                            spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(warp.X * 64 - viewport.X, warp.Y * 64 - viewport.Y, 64, 64), Color.Red * 0.75f);
                        }
                    }
                    mapDisplayDevice.BeginScene(spriteBatch);
                    try
                    {
                        currentLocation.Map.GetLayer("Front").Draw(mapDisplayDevice, viewport, Location.Origin, wrapAround: false, 4);
                    }
                    catch (KeyNotFoundException exception3)
                    {
                        CheckToReloadGameLocationAfterDrawFail.Invoke("Front", exception3);
                    }
                    mapDisplayDevice.EndScene();
                    currentLocation.drawAboveFrontLayer(spriteBatch);
                    if (currentLocation.tapToMove.targetNPC == null && (displayHUD || eventUp) && currentBillboard == 0 && gameMode == 3 && !freezeControls && !panMode && !HostPaused)
                    {
                        DrawTapToMoveTarget.Invoke();
                    }
                    _spriteBatchEnd.Invoke();
                    SetSpriteBatchBeginNextID("N");
                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                    if (displayFarmer && player.ActiveObject != null && (bool)player.ActiveObject.bigCraftable && checkBigCraftableBoundariesForFrontLayer() && currentLocation.Map.GetLayer("Front").PickTile(new Location(player.getStandingX(), player.getStandingY()), viewport.Size) == null)
                    {
                        drawPlayerHeldObject(player);
                    }
                    else if (displayFarmer && player.ActiveObject != null && ((currentLocation.Map.GetLayer("Front").PickTile(new Location((int)player.Position.X, (int)player.Position.Y - 38), viewport.Size) != null && !currentLocation.Map.GetLayer("Front").PickTile(new Location((int)player.Position.X, (int)player.Position.Y - 38), viewport.Size).TileIndexProperties.ContainsKey("FrontAlways")) || (currentLocation.Map.GetLayer("Front").PickTile(new Location(player.GetBoundingBox().Right, (int)player.Position.Y - 38), viewport.Size) != null && !currentLocation.Map.GetLayer("Front").PickTile(new Location(player.GetBoundingBox().Right, (int)player.Position.Y - 38), viewport.Size).TileIndexProperties.ContainsKey("FrontAlways"))))
                    {
                        drawPlayerHeldObject(player);
                    }
                    if ((player.UsingTool || pickingTool) && player.CurrentTool != null && (!player.CurrentTool.Name.Equals("Seeds") || pickingTool) && currentLocation.Map.GetLayer("Front").PickTile(new Location(player.getStandingX(), (int)player.Position.Y - 38), viewport.Size) != null && currentLocation.Map.GetLayer("Front").PickTile(new Location(player.getStandingX(), player.getStandingY()), viewport.Size) == null)
                    {
                        drawTool(player);
                    }
                    if (currentLocation.Map.GetLayer("AlwaysFront") != null)
                    {
                        mapDisplayDevice.BeginScene(spriteBatch);
                        try
                        {
                            currentLocation.Map.GetLayer("AlwaysFront").Draw(mapDisplayDevice, viewport, Location.Origin, wrapAround: false, 4);
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
                        switch ((int)(toolHold / 600f))
                        {
                            case -1:
                                color = Tool.copperColor;
                                break;
                            case 0:
                                color = Tool.steelColor;
                                break;
                            case 1:
                                color = Tool.goldColor;
                                break;
                            case 2:
                                color = Tool.iridiumColor;
                                break;
                        }
                        spriteBatch.Draw(littleEffect, new Microsoft.Xna.Framework.Rectangle((int)player.getLocalPosition(viewport).X - 2, (int)player.getLocalPosition(viewport).Y - ((!player.CurrentTool.Name.Equals("Watering Can")) ? 64 : 0) - 2, (int)(toolHold % 600f * 0.08f) + 4, 12), Color.Black);
                        spriteBatch.Draw(littleEffect, new Microsoft.Xna.Framework.Rectangle((int)player.getLocalPosition(viewport).X, (int)player.getLocalPosition(viewport).Y - ((!player.CurrentTool.Name.Equals("Watering Can")) ? 64 : 0), (int)(toolHold % 600f * 0.08f), 8), color);
                    }
                    if (WeatherDebrisManager.Instance.isDebrisWeather && currentLocation.IsOutdoors && !currentLocation.ignoreDebrisWeather && !currentLocation.Name.Equals("Desert"))
                    {
                        WeatherDebrisManager.Instance.Draw(spriteBatch);
                    }
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
                    if (RainManager.Instance.isRaining && currentLocation.IsOutdoors && !currentLocation.Name.Equals("Desert") && !(currentLocation is Summit) && (!eventUp || currentLocation.isTileOnMap(new Vector2(viewport.X / 64, viewport.Y / 64))))
                    {
                        RainManager.Instance.Draw(spriteBatch);
                    }
                    _spriteBatchEnd.Invoke();
                    SetSpriteBatchBeginNextID("O");
                    _spriteBatchBegin.Invoke(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                    if (eventUp && currentLocation.currentEvent != null)
                    {
                        currentLocation.currentEvent.drawAboveAlwaysFrontLayer(spriteBatch);
                        foreach (NPC actor3 in currentLocation.currentEvent.actors)
                        {
                            if (actor3.isEmoting)
                            {
                                Vector2 localPosition = actor3.getLocalPosition(viewport);
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
                        _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, lightingBlend.GetValue(), SamplerState.LinearClamp, null, null, null, null);
                        spriteBatch.Draw(lightmap, Vector2.Zero, lightmap.Bounds, Color.White, 0f, Vector2.Zero, options.lightingQuality / 2, SpriteEffects.None, 1f);
                        if (RainManager.Instance.isRaining && (bool)currentLocation.isOutdoors && !(currentLocation is Desert))
                        {
                            spriteBatch.Draw(staminaRect, graphics.GraphicsDevice.Viewport.Bounds, Color.OrangeRed * 0.45f);
                        }
                        _spriteBatchEnd.Invoke();
                    }
                    SetSpriteBatchBeginNextID("Q");
                    _spriteBatchBegin.Invoke(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, null);
                    events.RenderedWorld.RaiseEmpty();
                    if (drawGrid)
                    {
                        int num = -viewport.X % 64;
                        float num2 = -viewport.Y % 64;
                        for (int j = num; j < graphics.GraphicsDevice.Viewport.Width; j += 64)
                        {
                            spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle(j, (int)num2, 1, graphics.GraphicsDevice.Viewport.Height), Color.Red * 0.5f);
                        }
                        for (float num3 = num2; num3 < (float)graphics.GraphicsDevice.Viewport.Height; num3 += 64f)
                        {
                            spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle(num, (int)num3, graphics.GraphicsDevice.Viewport.Width, 1), Color.Red * 0.5f);
                        }
                    }
                    if ((displayHUD || eventUp) && currentBillboard == 0 && gameMode == 3 && !freezeControls && !panMode && !HostPaused)
                    {
                        _drawHUD.SetValue(true);
                        if (isOutdoorMapSmallerThanViewport())
                        {
                            spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, 0, -System.Math.Min(viewport.X, 4096), graphics.GraphicsDevice.Viewport.Height), Color.Black);
                            spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(-viewport.X + currentLocation.map.Layers[0].LayerWidth * 64, 0, System.Math.Min(4096, graphics.GraphicsDevice.Viewport.Width - (-viewport.X + currentLocation.map.Layers[0].LayerWidth * 64)), graphics.GraphicsDevice.Viewport.Height), Color.Black);
                        }
                        DrawGreenPlacementBounds.Invoke();
                    }
                }
                if (farmEvent != null)
                {
                    farmEvent.draw(spriteBatch);
                }
                if (dialogueUp && !nameSelectUp && !messagePause && (activeClickableMenu == null || !(activeClickableMenu is DialogueBox)))
                {
                    drawDialogueBox();
                }
                if (progressBar)
                {
                    spriteBatch.Draw(fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle((graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - dialogueWidth) / 2, graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 128, dialogueWidth, 32), Color.LightGray);
                    spriteBatch.Draw(staminaRect, new Microsoft.Xna.Framework.Rectangle((graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Width - dialogueWidth) / 2, graphics.GraphicsDevice.Viewport.GetTitleSafeArea().Bottom - 128, (int)(pauseAccumulator / pauseTime * (float)dialogueWidth), 32), Color.DimGray);
                }
                if (RainManager.Instance.isRaining && currentLocation != null && (bool)currentLocation.isOutdoors && !(currentLocation is Desert))
                {
                    spriteBatch.Draw(staminaRect, graphics.GraphicsDevice.Viewport.Bounds, Color.Blue * 0.2f);
                }
                if ((messagePause || globalFade) && dialogueUp)
                {
                    drawDialogueBox();
                }
                foreach (TemporaryAnimatedSprite screenOverlayTempSprite in screenOverlayTempSprites)
                {
                    screenOverlayTempSprite.draw(spriteBatch, localPosition: true);
                }
                if (debugMode)
                {
                    System.Text.StringBuilder debugStringBuilder = _debugStringBuilder.GetValue();
                    debugStringBuilder.Clear();
                    if (panMode)
                    {
                        debugStringBuilder.Append((getOldMouseX() + viewport.X) / 64);
                        debugStringBuilder.Append(",");
                        debugStringBuilder.Append((getOldMouseY() + viewport.Y) / 64);
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
                    debugStringBuilder.Append("debugOutput: ");
                    debugStringBuilder.Append(debugOutput);
                    spriteBatch.DrawString(smallFont, debugStringBuilder, new Vector2(base.GraphicsDevice.Viewport.GetTitleSafeArea().X, base.GraphicsDevice.Viewport.GetTitleSafeArea().Y + smallFont.LineSpacing * 8), Color.Red, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.09999999f);
                }
                if (showKeyHelp)
                {
                    spriteBatch.DrawString(smallFont, keyHelpString, new Vector2(64f, (float)(viewport.Height - 64 - (dialogueUp ? (192 + (isQuestion ? (questionChoices.Count * 64) : 0)) : 0)) - smallFont.MeasureString(keyHelpString).Y), Color.LightGray, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.9999999f);
                }
                if (activeClickableMenu != null)
                {
                    _drawActiveClickableMenu.SetValue(true);
                    events.RenderingActiveMenu.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_OnPreRenderGuiEvent.Raise();
#endif
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
#if !SMAPI_3_0_STRICT
                    events.Legacy_OnPostRenderGuiEvent.Raise();
#endif
                }
                else if (farmEvent != null)
                {
                    farmEvent.drawAboveEverything(spriteBatch);
                }
                if (HostPaused)
                {
                    string s = content.LoadString("Strings\\StringsFromCSFiles:DayTimeMoneyBox.cs.10378");
                    SpriteText.drawStringWithScrollBackground(spriteBatch, s, 96, 32);
                }
                events.Rendered.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                events.Legacy_OnPostRenderEvent.Raise();
#endif
                _spriteBatchEnd.Invoke();
                drawOverlays(spriteBatch);
                renderScreenBuffer(BlendState.Opaque);
                if (_drawHUD.GetValue())
                {
                    DrawDayTimeMoneyBox.Invoke();
                    SetSpriteBatchBeginNextID("A-C");
                    SpriteBatchBegin.Invoke(1f);
                    events.RenderingHud.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_OnPreRenderHudEvent.Raise();
#endif
                    DrawHUD();
                    if (currentLocation != null && !(activeClickableMenu is GameMenu) && !(activeClickableMenu is QuestLog))
                    {
                        currentLocation.drawAboveAlwaysFrontLayerText(spriteBatch);
                    }
                    DrawAfterMap.Invoke();
                    events.RenderedHud.RaiseEmpty();
#if !SMAPI_3_0_STRICT
                    events.Legacy_OnPostRenderHudEvent.Raise();
#endif
                    _spriteBatchEnd.Invoke();
                    if (tutorialManager != null)
                    {
                        SetSpriteBatchBeginNextID("A-D");
                        SpriteBatchBegin.Invoke(options.zoomLevel);
                        tutorialManager.draw(spriteBatch);
                        _spriteBatchEnd.Invoke();
                    }
                    DrawToolbar.Invoke();
                    DrawVirtualJoypad.Invoke();
                }
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
                }
                if (_drawHUD.GetValue() && hudMessages.Count > 0 && (!eventUp || isFestival()))
                {
                    SetSpriteBatchBeginNextID("A-F");
                    SpriteBatchBegin.Invoke(NativeZoomLevel);
                    DrawHUDMessages.Invoke();
                    _spriteBatchEnd.Invoke();
                }
                if (CurrentEvent != null && CurrentEvent.skippable && (activeClickableMenu == null || (activeClickableMenu != null && !(activeClickableMenu is MenuWithInventory))))
                {
                    SetSpriteBatchBeginNextID("A-G");
                    SpriteBatchBegin.Invoke(NativeZoomLevel);
                    CurrentEvent.DrawSkipButton(spriteBatch);
                    _spriteBatchEnd.Invoke();
                }
                DrawTutorialUI.Invoke();
            }

            
        }
    }
}
