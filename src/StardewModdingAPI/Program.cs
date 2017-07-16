using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Threading;
#if SMAPI_FOR_WINDOWS
using System.Management;
using System.Windows.Forms;
#endif
using Newtonsoft.Json;
using StardewModdingAPI.AssemblyRewriters;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Logging;
using StardewModdingAPI.Framework.Models;
using StardewModdingAPI.Framework.ModHelpers;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Framework.Serialisation;
using StardewValley;
using Monitor = StardewModdingAPI.Framework.Monitor;
using SObject = StardewValley.Object;

namespace StardewModdingAPI
{
    /// <summary>The main entry point for SMAPI, responsible for hooking into and launching the game.</summary>
    internal class Program : IDisposable
    {
        /*********
        ** Properties
        *********/
        /// <summary>The log file to which to write messages.</summary>
        private readonly LogFileManager LogFile;

        /// <summary>Manages console output interception.</summary>
        private readonly ConsoleInterceptionManager ConsoleManager = new ConsoleInterceptionManager();

        /// <summary>The core logger and monitor for SMAPI.</summary>
        private readonly Monitor Monitor;

        /// <summary>Tracks whether the game should exit immediately and any pending initialisation should be cancelled.</summary>
        private readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection = new Reflector();

        /// <summary>The underlying game instance.</summary>
        private SGame GameInstance;

        /// <summary>The underlying content manager.</summary>
        private SContentManager ContentManager => (SContentManager)this.GameInstance.Content;

        /// <summary>The SMAPI configuration settings.</summary>
        /// <remarks>This is initialised after the game starts.</remarks>
        private SConfig Settings;

        /// <summary>Tracks the installed mods.</summary>
        /// <remarks>This is initialised after the game starts.</remarks>
        private ModRegistry ModRegistry;

        /// <summary>Manages deprecation warnings.</summary>
        /// <remarks>This is initialised after the game starts.</remarks>
        private DeprecationManager DeprecationManager;

        /// <summary>Manages console commands.</summary>
        /// <remarks>This is initialised after the game starts.</remarks>
        private CommandManager CommandManager;

        /// <summary>Whether the game is currently running.</summary>
        private bool IsGameRunning;

        /// <summary>Whether the program has been disposed.</summary>
        private bool IsDisposed;


        /*********
        ** Public methods
        *********/
        /// <summary>The main entry point which hooks into and launches the game.</summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            Program.AssertMinimumCompatibility();

            // get flags from arguments
            bool writeToConsole = !args.Contains("--no-terminal");

            // get log path from arguments
            string logPath = null;
            {
                int pathIndex = Array.LastIndexOf(args, "--log-path") + 1;
                if (pathIndex >= 1 && args.Length >= pathIndex)
                {
                    logPath = args[pathIndex];
                    if (!Path.IsPathRooted(logPath))
                        logPath = Path.Combine(Constants.LogDir, logPath);
                }
            }
            if (string.IsNullOrWhiteSpace(logPath))
                logPath = Constants.DefaultLogPath;

            // load SMAPI
            using (Program program = new Program(writeToConsole, logPath))
                program.RunInteractively();
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="writeToConsole">Whether to output log messages to the console.</param>
        /// <param name="logPath">The full file path to which to write log messages.</param>
        public Program(bool writeToConsole, string logPath)
        {
            this.LogFile = new LogFileManager(logPath);
            this.Monitor = new Monitor("SMAPI", this.ConsoleManager, this.LogFile, this.CancellationTokenSource) { WriteToConsole = writeToConsole };
        }

        /// <summary>Launch SMAPI.</summary>
        [HandleProcessCorruptedStateExceptions, SecurityCritical] // let try..catch handle corrupted state exceptions
        public void RunInteractively()
        {
            // initialise SMAPI
            try
            {
                // init logging
                this.Monitor.Log($"SMAPI {Constants.ApiVersion} with Stardew Valley {Constants.GetGameDisplayVersion(Constants.GameVersion)} on {this.GetFriendlyPlatformName()}", LogLevel.Info);
                this.Monitor.Log($"Mods go here: {Constants.ModPath}");
#if SMAPI_1_x
                this.Monitor.Log("Preparing SMAPI...");
#endif

                // validate paths
                this.VerifyPath(Constants.ModPath);
                this.VerifyPath(Constants.LogDir);

                // validate game version
                if (Constants.GameVersion.IsOlderThan(Constants.MinimumGameVersion))
                {
                    this.Monitor.Log($"Oops! You're running Stardew Valley {Constants.GetGameDisplayVersion(Constants.GameVersion)}, but the oldest supported version is {Constants.GetGameDisplayVersion(Constants.MinimumGameVersion)}. Please update your game before using SMAPI. If you have the beta version on Steam, you may need to opt out to get the latest non-beta updates.", LogLevel.Error);
                    this.PressAnyKeyToExit();
                    return;
                }
                if (Constants.MaximumGameVersion != null && Constants.GameVersion.IsNewerThan(Constants.MaximumGameVersion))
                {
                    this.Monitor.Log($"Oops! You're running Stardew Valley {Constants.GetGameDisplayVersion(Constants.GameVersion)}, but this version of SMAPI is only compatible up to Stardew Valley {Constants.GetGameDisplayVersion(Constants.MaximumGameVersion)}. Please check for a newer version of SMAPI.", LogLevel.Error);
                    this.PressAnyKeyToExit();
                    return;
                }

                // add error handlers
#if SMAPI_FOR_WINDOWS
                Application.ThreadException += (sender, e) => this.Monitor.Log($"Critical thread exception: {e.Exception.GetLogSummary()}", LogLevel.Error);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
#endif
                AppDomain.CurrentDomain.UnhandledException += (sender, e) => this.Monitor.Log($"Critical app domain exception: {e.ExceptionObject}", LogLevel.Error);

                // override game
                this.GameInstance = new SGame(this.Monitor, this.Reflection);
                StardewValley.Program.gamePtr = this.GameInstance;

                // add exit handler
                new Thread(() =>
                {
                    this.CancellationTokenSource.Token.WaitHandle.WaitOne();
                    if (this.IsGameRunning)
                    {
                        try
                        {
                            File.WriteAllText(Constants.FatalCrashMarker, string.Empty);
                            File.Copy(this.LogFile.Path, Constants.FatalCrashLog, overwrite: true);
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"SMAPI failed trying to track the crash details: {ex.GetLogSummary()}");
                        }

                        this.GameInstance.Exit();
                    }
                }).Start();

                // hook into game events
#if SMAPI_FOR_WINDOWS
                ((Form)Control.FromHandle(this.GameInstance.Window.Handle)).FormClosing += (sender, args) => this.Dispose();
#endif
                this.GameInstance.Exiting += (sender, e) => this.Dispose();
                this.GameInstance.Window.ClientSizeChanged += (sender, e) => GraphicsEvents.InvokeResize(this.Monitor, sender, e);
                GameEvents.InitializeInternal += (sender, e) => this.InitialiseAfterGameStart();
                GameEvents.GameLoadedInternal += (sender, e) => this.CheckForUpdateAsync();
                ContentEvents.AfterLocaleChanged += (sender, e) => this.OnLocaleChanged();

                // set window titles
                this.GameInstance.Window.Title = $"Stardew Valley {Constants.GetGameDisplayVersion(Constants.GameVersion)} - running SMAPI {Constants.ApiVersion}";
                Console.Title = $"SMAPI {Constants.ApiVersion} - running Stardew Valley {Constants.GetGameDisplayVersion(Constants.GameVersion)}";
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"SMAPI failed to initialise: {ex.GetLogSummary()}", LogLevel.Error);
                this.PressAnyKeyToExit();
                return;
            }

            // show details if game crashed during last session
            if (File.Exists(Constants.FatalCrashMarker))
            {
                this.Monitor.Log("The game crashed last time you played. That can be due to bugs in the game, but if it happens repeatedly you can ask for help here: http://community.playstarbound.com/threads/108375/.", LogLevel.Error);
                this.Monitor.Log($"If you ask for help, make sure to attach this file: {Constants.FatalCrashLog}", LogLevel.Error);
                this.Monitor.Log("Press any key to delete the crash data and continue playing.", LogLevel.Info);
                Console.ReadKey();
                File.Delete(Constants.FatalCrashLog);
                File.Delete(Constants.FatalCrashMarker);
            }

            // start game
#if SMAPI_1_x
            this.Monitor.Log("Starting game...");
#else
            this.Monitor.Log("Starting game...", LogLevel.Trace);
#endif
            try
            {
                this.IsGameRunning = true;
                this.GameInstance.Run();
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"The game failed unexpectedly: {ex.GetLogSummary()}", LogLevel.Error);
                this.PressAnyKeyToExit();
            }
            finally
            {
                this.Dispose();
            }
        }

#if SMAPI_1_x
        /// <summary>Get a monitor for legacy code which doesn't have one passed in.</summary>
        [Obsolete("This method should only be used when needed for backwards compatibility.")]
        internal IMonitor GetLegacyMonitorForMod()
        {
            string modName = this.ModRegistry.GetModFromStack() ?? "unknown";
            return this.GetSecondaryMonitor(modName);
        }
#endif

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            this.Monitor.Log("Disposing...", LogLevel.Trace);

            // skip if already disposed
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;

            // dispose mod data
            foreach (IModMetadata mod in this.ModRegistry.GetMods())
            {
                try
                {
                    (mod.Mod as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"The {mod.DisplayName} mod failed during disposal: {ex.GetLogSummary()}.", LogLevel.Warn);
                }
            }

            // dispose core components
            this.IsGameRunning = false;
            this.LogFile?.Dispose();
            this.ConsoleManager?.Dispose();
            this.CancellationTokenSource?.Dispose();
            this.GameInstance?.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Assert that the minimum conditions are present to initialise SMAPI without type load exceptions.</summary>
        private static void AssertMinimumCompatibility()
        {
            void PrintErrorAndExit(string message)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message);
                Console.ResetColor();
                Program.PressAnyKeyToExit(showMessage: true);
            }

            // get game assembly name
            const string gameAssemblyName =
#if SMAPI_FOR_WINDOWS
                "Stardew Valley";
#else
                "StardewValley";
#endif

            // game not present
            if (Type.GetType($"StardewValley.Game1, {gameAssemblyName}", throwOnError: false) == null)
            {
                PrintErrorAndExit(
                    "Oops! SMAPI can't find the game. "
                    + (Assembly.GetCallingAssembly().Location?.Contains(Path.Combine("internal", "Windows")) == true || Assembly.GetCallingAssembly().Location?.Contains(Path.Combine("internal", "Mono")) == true
                        ? "It looks like you're running SMAPI from the download package, but you need to run the installed version instead. "
                        : "Make sure you're running StardewModdingAPI.exe in your game folder. "
                    )
                    + "See the readme.txt file for details."
                );
            }

            // Stardew Valley 1.2 types not present
            if (Type.GetType($"StardewValley.LocalizedContentManager+LanguageCode, {gameAssemblyName}", throwOnError: false) == null)
            {
                PrintErrorAndExit(Constants.GameVersion.IsOlderThan(Constants.MinimumGameVersion)
                    ? $"Oops! You're running Stardew Valley {Constants.GetGameDisplayVersion(Constants.GameVersion)}, but the oldest supported version is {Constants.GetGameDisplayVersion(Constants.MinimumGameVersion)}. Please update your game before using SMAPI."
                    : "Oops! SMAPI doesn't seem to be compatible with your game. Make sure you're running the latest version of Stardew Valley and SMAPI."
                );
            }
        }

        /// <summary>Initialise SMAPI and mods after the game starts.</summary>
        private void InitialiseAfterGameStart()
        {
            // load settings
            this.Settings = JsonConvert.DeserializeObject<SConfig>(File.ReadAllText(Constants.ApiConfigPath));
            this.GameInstance.VerboseLogging = this.Settings.VerboseLogging;

            // load core components
            this.ModRegistry = new ModRegistry();
            this.DeprecationManager = new DeprecationManager(this.Monitor, this.ModRegistry);
            this.CommandManager = new CommandManager();

#if SMAPI_1_x
            // inject compatibility shims
#pragma warning disable 618
            Command.Shim(this.CommandManager, this.DeprecationManager, this.ModRegistry);
            Config.Shim(this.DeprecationManager);
            Log.Shim(this.DeprecationManager, this.GetSecondaryMonitor("legacy mod"), this.ModRegistry);
            Mod.Shim(this.DeprecationManager);
            GameEvents.Shim(this.DeprecationManager);
            PlayerEvents.Shim(this.DeprecationManager);
            TimeEvents.Shim(this.DeprecationManager);
#pragma warning restore 618
#endif

            // redirect direct console output
            {
                Monitor monitor = this.GetSecondaryMonitor("Console.Out");
                if (monitor.WriteToConsole)
                    this.ConsoleManager.OnMessageIntercepted += message => this.HandleConsoleMessage(monitor, message);
            }

            // add headers
            if (this.Settings.DeveloperMode)
            {
                this.Monitor.ShowTraceInConsole = true;
#if !SMAPI_1_x
                this.Monitor.ShowFullStampInConsole = true;
#endif
                this.Monitor.Log($"You configured SMAPI to run in developer mode. The console may be much more verbose. You can disable developer mode by installing the non-developer version of SMAPI, or by editing {Constants.ApiConfigPath}.", LogLevel.Info);
            }
            if (!this.Settings.CheckForUpdates)
                this.Monitor.Log($"You configured SMAPI to not check for updates. Running an old version of SMAPI is not recommended. You can enable update checks by reinstalling SMAPI or editing {Constants.ApiConfigPath}.", LogLevel.Warn);
            if (!this.Monitor.WriteToConsole)
                this.Monitor.Log("Writing to the terminal is disabled because the --no-terminal argument was received. This usually means launching the terminal failed.", LogLevel.Warn);
            if (this.Settings.VerboseLogging)
                this.Monitor.Log("Verbose logging enabled.", LogLevel.Trace);

            // validate XNB integrity
            if (!this.ValidateContentIntegrity())
                this.Monitor.Log("SMAPI found problems in your game's content files which are likely to cause errors or crashes. Consider uninstalling XNB mods or reinstalling the game.", LogLevel.Error);

            // load mods
            {
#if SMAPI_1_x
                this.Monitor.Log("Loading mod metadata...");
#else
                this.Monitor.Log("Loading mod metadata...", LogLevel.Trace);
#endif
                ModResolver resolver = new ModResolver();

                // load manifests
                IModMetadata[] mods = resolver.ReadManifests(Constants.ModPath, new JsonHelper(), this.Settings.ModCompatibility, this.Settings.DisabledMods).ToArray();
                resolver.ValidateManifests(mods, Constants.ApiVersion);

                // check for deprecated metadata
#if SMAPI_1_x
                IList<Action> deprecationWarnings = new List<Action>();
                foreach (IModMetadata mod in mods.Where(m => m.Status != ModMetadataStatus.Failed))
                {
                    // missing fields that will be required in SMAPI 2.0
                    {
                        List<string> missingFields = new List<string>(3);

                        if (string.IsNullOrWhiteSpace(mod.Manifest.Name))
                            missingFields.Add(nameof(IManifest.Name));
                        if (mod.Manifest.Version == null || mod.Manifest.Version.ToString() == "0.0")
                            missingFields.Add(nameof(IManifest.Version));
                        if (string.IsNullOrWhiteSpace(mod.Manifest.UniqueID))
                            missingFields.Add(nameof(IManifest.UniqueID));

                        if (missingFields.Any())
                            deprecationWarnings.Add(() => this.Monitor.Log($"{mod.DisplayName} is missing some manifest fields ({string.Join(", ", missingFields)}) which will be required in an upcoming SMAPI version.", LogLevel.Warn));
                    }

                    // per-save directories
                    if ((mod.Manifest as Manifest)?.PerSaveConfigs == true)
                    {
                        deprecationWarnings.Add(() => this.DeprecationManager.Warn(mod.DisplayName, $"{nameof(Manifest)}.{nameof(Manifest.PerSaveConfigs)}", "1.0", DeprecationLevel.PendingRemoval));
                        try
                        {
                            string psDir = Path.Combine(mod.DirectoryPath, "psconfigs");
                            Directory.CreateDirectory(psDir);
                            if (!Directory.Exists(psDir))
                                mod.SetStatus(ModMetadataStatus.Failed, "it requires per-save configuration files ('psconfigs') which couldn't be created for some reason.");
                        }
                        catch (Exception ex)
                        {
                            mod.SetStatus(ModMetadataStatus.Failed, $"it requires per-save configuration files ('psconfigs') which couldn't be created: {ex.GetLogSummary()}");
                        }
                    }
                }
#endif

                // process dependencies
                mods = resolver.ProcessDependencies(mods).ToArray();

                // load mods
#if SMAPI_1_x
                this.LoadMods(mods, new JsonHelper(), this.ContentManager, deprecationWarnings);
                foreach (Action warning in deprecationWarnings)
                    warning();
#else
                this.LoadMods(mods, new JsonHelper(), this.ContentManager);
#endif
            }
            if (this.Monitor.IsExiting)
            {
                this.Monitor.Log("SMAPI shutting down: aborting initialisation.", LogLevel.Warn);
                return;
            }

            // update window titles
            int modsLoaded = this.ModRegistry.GetMods().Count();
            this.GameInstance.Window.Title = $"Stardew Valley {Constants.GetGameDisplayVersion(Constants.GameVersion)} - running SMAPI {Constants.ApiVersion} with {modsLoaded} mods";
            Console.Title = $"SMAPI {Constants.ApiVersion} - running Stardew Valley {Constants.GetGameDisplayVersion(Constants.GameVersion)} with {modsLoaded} mods";

            // start SMAPI console
            new Thread(this.RunConsoleLoop).Start();
        }

        /// <summary>Handle the game changing locale.</summary>
        private void OnLocaleChanged()
        {
            // get locale
            string locale = this.ContentManager.GetLocale();
            LocalizedContentManager.LanguageCode languageCode = this.ContentManager.GetCurrentLanguage();

            // update mod translation helpers
            foreach (IModMetadata mod in this.ModRegistry.GetMods())
                (mod.Mod.Helper.Translation as TranslationHelper)?.SetLocale(locale, languageCode);
        }

        /// <summary>Run a loop handling console input.</summary>
        [SuppressMessage("ReSharper", "FunctionNeverReturns", Justification = "The thread is aborted when the game exits.")]
        private void RunConsoleLoop()
        {
            // prepare console
#if SMAPI_1_x
            this.Monitor.Log("Starting console...");
#endif
            this.Monitor.Log("Type 'help' for help, or 'help <cmd>' for a command's usage", LogLevel.Info);
            this.CommandManager.Add("SMAPI", "help", "Lists command documentation.\n\nUsage: help\nLists all available commands.\n\nUsage: help <cmd>\n- cmd: The name of a command whose documentation to display.", this.HandleCommand);
            this.CommandManager.Add("SMAPI", "reload_i18n", "Reloads translation files for all mods.\n\nUsage: reload_i18n", this.HandleCommand);

            // start handling command line input
            Thread inputThread = new Thread(() =>
            {
                while (true)
                {
                    // get input
                    string input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    // parse input
                    try
                    {
                        if (!this.CommandManager.Trigger(input))
                            this.Monitor.Log("Unknown command; type 'help' for a list of available commands.", LogLevel.Error);
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"The handler registered for that command failed:\n{ex.GetLogSummary()}", LogLevel.Error);
                    }
                }
            });
            inputThread.Start();

            // keep console thread alive while the game is running
            while (this.IsGameRunning && !this.Monitor.IsExiting)
                Thread.Sleep(1000 / 10);
            if (inputThread.ThreadState == ThreadState.Running)
                inputThread.Abort();
        }

        /// <summary>Look for common issues with the game's XNB content, and log warnings if anything looks broken or outdated.</summary>
        /// <returns>Returns whether all integrity checks passed.</returns>
        private bool ValidateContentIntegrity()
        {
            this.Monitor.Log("Detecting common issues...", LogLevel.Trace);
            bool issuesFound = false;

            // object format (commonly broken by outdated files)
            {
                // detect issues
                bool hasObjectIssues = false;
                void LogIssue(int id, string issue) => this.Monitor.Log($@"Detected issue: item #{id} in Content\Data\ObjectInformation.xnb is invalid ({issue}).", LogLevel.Trace);
                foreach (KeyValuePair<int, string> entry in Game1.objectInformation)
                {
                    // must not be empty
                    if (string.IsNullOrWhiteSpace(entry.Value))
                    {
                        LogIssue(entry.Key, "entry is empty");
                        hasObjectIssues = true;
                        continue;
                    }

                    // require core fields
                    string[] fields = entry.Value.Split('/');
                    if (fields.Length < SObject.objectInfoDescriptionIndex + 1)
                    {
                        LogIssue(entry.Key, "too few fields for an object");
                        hasObjectIssues = true;
                        continue;
                    }

                    // check min length for specific types
                    switch (fields[SObject.objectInfoTypeIndex].Split(new[] { ' ' }, 2)[0])
                    {
                        case "Cooking":
                            if (fields.Length < SObject.objectInfoBuffDurationIndex + 1)
                            {
                                LogIssue(entry.Key, "too few fields for a cooking item");
                                hasObjectIssues = true;
                            }
                            break;
                    }
                }

                // log error
                if (hasObjectIssues)
                {
                    issuesFound = true;
                    this.Monitor.Log(@"Your Content\Data\ObjectInformation.xnb file seems to be broken or outdated.", LogLevel.Warn);
                }
            }

            return !issuesFound;
        }

        /// <summary>Asynchronously check for a new version of SMAPI, and print a message to the console if an update is available.</summary>
        private void CheckForUpdateAsync()
        {
            if (!this.Settings.CheckForUpdates)
                return;

            new Thread(() =>
            {
                try
                {
                    GitRelease release = UpdateHelper.GetLatestVersionAsync(Constants.GitHubRepository).Result;
                    ISemanticVersion latestVersion = new SemanticVersion(release.Tag);
                    if (latestVersion.IsNewerThan(Constants.ApiVersion))
                        this.Monitor.Log($"You can update SMAPI from version {Constants.ApiVersion} to {latestVersion}", LogLevel.Alert);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Couldn't check for a new version of SMAPI. This won't affect your game, but you may not be notified of new versions if this keeps happening.\n{ex.GetLogSummary()}");
                }
            }).Start();
        }

        /// <summary>Create a directory path if it doesn't exist.</summary>
        /// <param name="path">The directory path.</param>
        private void VerifyPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Couldn't create a path: {path}\n\n{ex.GetLogSummary()}", LogLevel.Error);
            }
        }

        /// <summary>Load and hook up the given mods.</summary>
        /// <param name="mods">The mods to load.</param>
        /// <param name="jsonHelper">The JSON helper with which to read mods' JSON files.</param>
        /// <param name="contentManager">The content manager to use for mod content.</param>
#if SMAPI_1_x
        /// <param name="deprecationWarnings">A list to populate with any deprecation warnings.</param>
        private void LoadMods(IModMetadata[] mods, JsonHelper jsonHelper, SContentManager contentManager, IList<Action> deprecationWarnings)
#else
        private void LoadMods(IModMetadata[] mods, JsonHelper jsonHelper, SContentManager contentManager)
#endif
        {
#if SMAPI_1_x
            this.Monitor.Log("Loading mods...");
#else
            this.Monitor.Log("Loading mods...", LogLevel.Trace);
#endif
            // load mod assemblies
            IDictionary<IModMetadata, string> skippedMods = new Dictionary<IModMetadata, string>();
            {
                void TrackSkip(IModMetadata mod, string reasonPhrase) => skippedMods[mod] = reasonPhrase;

                AssemblyLoader modAssemblyLoader = new AssemblyLoader(Constants.TargetPlatform, this.Monitor);
                AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => modAssemblyLoader.ResolveAssembly(e.Name);
                foreach (IModMetadata metadata in mods)
                {
                    // get basic info
                    IManifest manifest = metadata.Manifest;
                    string assemblyPath = metadata.Manifest?.EntryDll != null
                        ? Path.Combine(metadata.DirectoryPath, metadata.Manifest.EntryDll)
                        : null;
                    this.Monitor.Log(assemblyPath != null
                        ? $"Loading {metadata.DisplayName} from {assemblyPath.Replace(Constants.ModPath, "").TrimStart(Path.DirectorySeparatorChar)}..."
                        : $"Loading {metadata.DisplayName}...", LogLevel.Trace);

                    // validate status
                    if (metadata.Status == ModMetadataStatus.Failed)
                    {
                        this.Monitor.Log($"   Failed: {metadata.Error}", LogLevel.Trace);
                        TrackSkip(metadata, metadata.Error);
                        continue;
                    }

                    // preprocess & load mod assembly
                    Assembly modAssembly;
                    try
                    {
                        modAssembly = modAssemblyLoader.Load(assemblyPath, assumeCompatible: metadata.Compatibility?.Compatibility == ModCompatibilityType.AssumeCompatible);
                    }
                    catch (IncompatibleInstructionException ex)
                    {
                        TrackSkip(metadata, $"it's not compatible with the latest version of the game or SMAPI (detected {ex.NounPhrase}). Please check for a newer version of the mod.");
                        continue;
                    }
                    catch (Exception ex)
                    {
                        TrackSkip(metadata, $"its DLL '{manifest.EntryDll}' couldn't be loaded:\n{ex.GetLogSummary()}");
                        continue;
                    }

                    // validate assembly
                    try
                    {
                        int modEntries = modAssembly.DefinedTypes.Count(type => typeof(Mod).IsAssignableFrom(type) && !type.IsAbstract);
                        if (modEntries == 0)
                        {
                            TrackSkip(metadata, $"its DLL has no '{nameof(Mod)}' subclass.");
                            continue;
                        }
                        if (modEntries > 1)
                        {
                            TrackSkip(metadata, $"its DLL contains multiple '{nameof(Mod)}' subclasses.");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        TrackSkip(metadata, $"its DLL couldn't be loaded:\n{ex.GetLogSummary()}");
                        continue;
                    }

                    // initialise mod
                    try
                    {
                        // get implementation
                        TypeInfo modEntryType = modAssembly.DefinedTypes.First(type => typeof(Mod).IsAssignableFrom(type) && !type.IsAbstract);
                        Mod mod = (Mod)modAssembly.CreateInstance(modEntryType.ToString());
                        if (mod == null)
                        {
                            TrackSkip(metadata, "its entry class couldn't be instantiated.");
                            continue;
                        }

#if SMAPI_1_x
                        // prevent mods from using SMAPI 2.0 content interception before release
                        // ReSharper disable SuspiciousTypeConversion.Global
                        if (mod is IAssetEditor || mod is IAssetLoader)
                        {
                            TrackSkip(metadata, $"its entry class implements {nameof(IAssetEditor)} or {nameof(IAssetLoader)}. These are part of a prototype API that isn't available for mods to use yet.");
                        }
                        // ReSharper restore SuspiciousTypeConversion.Global
#endif

                        // inject data
                        {
                            ICommandHelper commandHelper = new CommandHelper(manifest.UniqueID, metadata.DisplayName, this.CommandManager);
                            IContentHelper contentHelper = new ContentHelper(contentManager, metadata.DirectoryPath, manifest.UniqueID, metadata.DisplayName);
                            IReflectionHelper reflectionHelper = new ReflectionHelper(manifest.UniqueID, this.Reflection);
                            IModRegistry modRegistryHelper = new ModRegistryHelper(manifest.UniqueID, this.ModRegistry);
                            ITranslationHelper translationHelper = new TranslationHelper(manifest.UniqueID, manifest.Name, contentManager.GetLocale(), contentManager.GetCurrentLanguage());

                            mod.ModManifest = manifest;
                            mod.Helper = new ModHelper(manifest.UniqueID, metadata.DirectoryPath, jsonHelper, contentHelper, commandHelper, modRegistryHelper, reflectionHelper, translationHelper);
                            mod.Monitor = this.GetSecondaryMonitor(metadata.DisplayName);
#if SMAPI_1_x
                            mod.PathOnDisk = metadata.DirectoryPath;
#endif
                        }

                        // track mod
                        metadata.SetMod(mod);
                        this.ModRegistry.Add(metadata);
                    }
                    catch (Exception ex)
                    {
                        TrackSkip(metadata, $"initialisation failed:\n{ex.GetLogSummary()}");
                    }
                }
            }
            IModMetadata[] loadedMods = this.ModRegistry.GetMods().ToArray();

            // log skipped mods
#if !SMAPI_1_x
            this.Monitor.Newline();
#endif
            if (skippedMods.Any())
            {
                this.Monitor.Log($"Skipped {skippedMods.Count} mods:", LogLevel.Error);
                foreach (var pair in skippedMods.OrderBy(p => p.Key.DisplayName))
                {
                    IModMetadata mod = pair.Key;
                    string reason = pair.Value;

                    if (mod.Manifest?.Version != null)
                        this.Monitor.Log($"   {mod.DisplayName} {mod.Manifest.Version} because {reason}", LogLevel.Error);
                    else
                        this.Monitor.Log($"   {mod.DisplayName} because {reason}", LogLevel.Error);
                }
#if !SMAPI_1_x
                this.Monitor.Newline();
#endif
            }

            // log loaded mods
            this.Monitor.Log($"Loaded {loadedMods.Length} mods" + (loadedMods.Length > 0 ? ":" : "."), LogLevel.Info);
            foreach (IModMetadata metadata in loadedMods.OrderBy(p => p.DisplayName))
            {
                IManifest manifest = metadata.Manifest;
                this.Monitor.Log(
                    $"   {metadata.DisplayName} {manifest.Version}"
                        + (!string.IsNullOrWhiteSpace(manifest.Author) ? $" by {manifest.Author}" : "")
                        + (!string.IsNullOrWhiteSpace(manifest.Description) ? $" | {manifest.Description}" : ""),
                    LogLevel.Info
                );
            }
#if !SMAPI_1_x
            this.Monitor.Newline();
#endif

            // initialise translations
            this.ReloadTranslations();

            // initialise loaded mods
            foreach (IModMetadata metadata in loadedMods)
            {
                // add interceptors
                if (metadata.Mod.Helper.Content is ContentHelper helper)
                {
                    this.ContentManager.Editors[metadata] = helper.ObservableAssetEditors;
                    this.ContentManager.Loaders[metadata] = helper.ObservableAssetLoaders;
                }

                // call entry method
                try
                {
                    IMod mod = metadata.Mod;
                    mod.Entry(mod.Helper);
#if SMAPI_1_x
                    (mod as Mod)?.Entry(); // deprecated since 1.0

                    // raise deprecation warning for old Entry() methods
                    if (this.DeprecationManager.IsVirtualMethodImplemented(mod.GetType(), typeof(Mod), nameof(Mod.Entry), new[] { typeof(object[]) }))
                        deprecationWarnings.Add(() => this.DeprecationManager.Warn(metadata.DisplayName, $"{nameof(Mod)}.{nameof(Mod.Entry)}(object[]) instead of {nameof(Mod)}.{nameof(Mod.Entry)}({nameof(IModHelper)})", "1.0", DeprecationLevel.PendingRemoval));
#endif
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"{metadata.DisplayName} failed on entry and might not work correctly. Technical details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }
            }

            // reset cache when needed
            // only register listeners after Entry to avoid repeatedly reloading assets during load
            foreach (IModMetadata metadata in loadedMods)
            {
                if (metadata.Mod.Helper.Content is ContentHelper helper)
                {
                    helper.ObservableAssetEditors.CollectionChanged += (sender, e) =>
                    {
                        if (e.NewItems.Count > 0)
                            this.ContentManager.Reset();
                    };
                    helper.ObservableAssetLoaders.CollectionChanged += (sender, e) =>
                    {
                        if (e.NewItems.Count > 0)
                            this.ContentManager.Reset();
                    };
                }
            }
            this.ContentManager.Reset();
        }

        /// <summary>Reload translations for all mods.</summary>
        private void ReloadTranslations()
        {
            JsonHelper jsonHelper = new JsonHelper();
            foreach (IModMetadata metadata in this.ModRegistry.GetMods())
            {
                // read translation files
                IDictionary<string, IDictionary<string, string>> translations = new Dictionary<string, IDictionary<string, string>>();
                DirectoryInfo translationsDir = new DirectoryInfo(Path.Combine(metadata.DirectoryPath, "i18n"));
                if (translationsDir.Exists)
                {
                    foreach (FileInfo file in translationsDir.EnumerateFiles("*.json"))
                    {
                        string locale = Path.GetFileNameWithoutExtension(file.Name.ToLower().Trim());
                        try
                        {
                            translations[locale] = jsonHelper.ReadJsonFile<IDictionary<string, string>>(file.FullName);
                        }
                        catch (Exception ex)
                        {
                            this.Monitor.Log($"Couldn't read {metadata.DisplayName}'s i18n/{locale}.json file: {ex.GetLogSummary()}");
                        }
                    }
                }

                // update translation
                TranslationHelper translationHelper = (TranslationHelper)metadata.Mod.Helper.Translation;
                translationHelper.SetTranslations(translations);
            }
        }

        /// <summary>The method called when the user submits a core SMAPI command in the console.</summary>
        /// <param name="name">The command name.</param>
        /// <param name="arguments">The command arguments.</param>
        private void HandleCommand(string name, string[] arguments)
        {
            switch (name)
            {
                case "help":
                    if (arguments.Any())
                    {
                        Framework.Command result = this.CommandManager.Get(arguments[0]);
                        if (result == null)
                            this.Monitor.Log("There's no command with that name.", LogLevel.Error);
                        else
                            this.Monitor.Log($"{result.Name}: {result.Documentation}\n(Added by {result.ModName}.)", LogLevel.Info);
                    }
                    else
                    {
#if SMAPI_1_x
                        this.Monitor.Log("The following commands are registered: " + string.Join(", ", this.CommandManager.GetAll().Select(p => p.Name)) + ".", LogLevel.Info);
                        this.Monitor.Log("For more information about a command, type 'help command_name'.", LogLevel.Info);
#else
                        string message = "The following commands are registered:\n";
                        IGrouping<string, string>[] groups = (from command in this.CommandManager.GetAll() orderby command.ModName, command.Name group command.Name by command.ModName).ToArray();
                        foreach (var group in groups)
                        {
                            string modName = group.Key;
                            string[] commandNames = group.ToArray();
                            message += $"{modName}:\n  {string.Join("\n  ", commandNames)}\n\n";
                        }
                        message += "For more information about a command, type 'help command_name'.";

                        this.Monitor.Log(message, LogLevel.Info);
#endif
                    }
                    break;

                case "reload_i18n":
                    this.ReloadTranslations();
                    this.Monitor.Log("Reloaded translation files for all mods. This only affects new translations the mods fetch; if they cached some text, it may not be updated.", LogLevel.Info);
                    break;

                default:
                    throw new NotSupportedException($"Unrecognise core SMAPI command '{name}'.");
            }
        }

        /// <summary>Redirect messages logged directly to the console to the given monitor.</summary>
        /// <param name="monitor">The monitor with which to log messages.</param>
        /// <param name="message">The message to log.</param>
        private void HandleConsoleMessage(IMonitor monitor, string message)
        {
            LogLevel level = message.Contains("Exception") ? LogLevel.Error : LogLevel.Trace; // intercept potential exceptions
            monitor.Log(message, level);
        }

        /// <summary>Show a 'press any key to exit' message, and exit when they press a key.</summary>
        private void PressAnyKeyToExit()
        {
            this.Monitor.Log("Game has ended. Press any key to exit.", LogLevel.Info);
            Program.PressAnyKeyToExit(showMessage: false);
        }

        /// <summary>Show a 'press any key to exit' message, and exit when they press a key.</summary>
        /// <param name="showMessage">Whether to print a 'press any key to exit' message to the console.</param>
        private static void PressAnyKeyToExit(bool showMessage)
        {
            if (showMessage)
                Console.WriteLine("Game has ended. Press any key to exit.");
            Thread.Sleep(100);
            Console.ReadKey();
            Environment.Exit(0);
        }

        /// <summary>Get a monitor instance derived from SMAPI's current settings.</summary>
        /// <param name="name">The name of the module which will log messages with this instance.</param>
        private Monitor GetSecondaryMonitor(string name)
        {
            return new Monitor(name, this.ConsoleManager, this.LogFile, this.CancellationTokenSource)
            {
                WriteToConsole = this.Monitor.WriteToConsole,
                ShowTraceInConsole = this.Settings.DeveloperMode,
#if !SMAPI_1_x
                ShowFullStampInConsole = this.Settings.DeveloperMode
#endif
            };
        }

        /// <summary>Get a human-readable name for the current platform.</summary>
        [SuppressMessage("ReSharper", "EmptyGeneralCatchClause", Justification = "Error suppressed deliberately to fallback to default behaviour.")]
        private string GetFriendlyPlatformName()
        {
#if SMAPI_FOR_WINDOWS
            try
            {
                return new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem")
                    .Get()
                    .Cast<ManagementObject>()
                    .Select(entry => entry.GetPropertyValue("Caption").ToString())
                    .FirstOrDefault();
            }
            catch { }
#endif
            return Environment.OSVersion.ToString();
        }
    }
}
