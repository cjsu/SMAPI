using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.AssemblyRewriters;
using StardewModdingAPI.AssemblyRewriters.Finders;
using StardewModdingAPI.AssemblyRewriters.Rewriters;
using StardewModdingAPI.AssemblyRewriters.Rewriters.Wrappers;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewModdingAPI
{
    /// <summary>Contains SMAPI's constants and assumptions.</summary>
    public static class Constants
    {
        /*********
        ** Properties
        *********/
        /// <summary>The directory path containing the current save's data (if a save is loaded).</summary>
        private static string RawSavePath => Context.IsSaveLoaded ? Path.Combine(Constants.SavesPath, Constants.GetSaveFolderName()) : null;

        /// <summary>Whether the directory containing the current save's data exists on disk.</summary>
        private static bool SavePathReady => Context.IsSaveLoaded && Directory.Exists(Constants.RawSavePath);


        /*********
        ** Accessors
        *********/
        /****
        ** Public
        ****/
        /// <summary>SMAPI's current semantic version.</summary>
        public static ISemanticVersion ApiVersion { get; } =
#if SMAPI_1_x
            new SemanticVersion(1, 15, 1); // alpha-{DateTime.UtcNow:yyyyMMddHHmm}
#else
            new SemanticVersion(2, 0, 0, $"alpha-{DateTime.UtcNow:yyyyMMddHHmm}");
#endif

        /// <summary>The minimum supported version of Stardew Valley.</summary>
        public static ISemanticVersion MinimumGameVersion { get; } = new SemanticVersion("1.2.30");

        /// <summary>The maximum supported version of Stardew Valley.</summary>
        public static ISemanticVersion MaximumGameVersion { get; } = null;

        /// <summary>The path to the game folder.</summary>
        public static string ExecutionPath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>The directory path containing Stardew Valley's app data.</summary>
        public static string DataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");

        /// <summary>The directory path in which error logs should be stored.</summary>
        public static string LogDir { get; } = Path.Combine(Constants.DataPath, "ErrorLogs");

        /// <summary>The directory path where all saves are stored.</summary>
        public static string SavesPath { get; } = Path.Combine(Constants.DataPath, "Saves");

        /// <summary>The directory name containing the current save's data (if a save is loaded and the directory exists).</summary>
        public static string SaveFolderName => Context.IsSaveLoaded ? Constants.GetSaveFolderName() : "";

        /// <summary>The directory path containing the current save's data (if a save is loaded and the directory exists).</summary>
        public static string CurrentSavePath => Constants.SavePathReady ? Path.Combine(Constants.SavesPath, Constants.GetSaveFolderName()) : "";

        /****
        ** Internal
        ****/
        /// <summary>The GitHub repository to check for updates.</summary>
        internal const string GitHubRepository = "Pathoschild/SMAPI";

        /// <summary>The file path for the SMAPI configuration file.</summary>
        internal static string ApiConfigPath => Path.Combine(Constants.ExecutionPath, $"{typeof(Program).Assembly.GetName().Name}.config.json");

        /// <summary>The file path to the log where the latest output should be saved.</summary>
        internal static string DefaultLogPath => Path.Combine(Constants.LogDir, "SMAPI-latest.txt");

        /// <summary>A copy of the log leading up to the previous fatal crash, if any.</summary>
        internal static string FatalCrashLog => Path.Combine(Constants.LogDir, "SMAPI-crash.txt");

        /// <summary>The file path which stores a fatal crash message for the next run.</summary>
        internal static string FatalCrashMarker => Path.Combine(Constants.ExecutionPath, "StardewModdingAPI.crash.marker");

        /// <summary>The full path to the folder containing mods.</summary>
        internal static string ModPath { get; } = Path.Combine(Constants.ExecutionPath, "Mods");

        /// <summary>The game's current semantic version.</summary>
        internal static ISemanticVersion GameVersion { get; } = Constants.GetGameVersion();

        /// <summary>The target game platform.</summary>
        internal static Platform TargetPlatform { get; } =
#if SMAPI_FOR_WINDOWS
        Platform.Windows;
#else
        Platform.Mono;
#endif


        /*********
        ** Protected methods
        *********/
        /// <summary>Get metadata for mapping assemblies to the current platform.</summary>
        /// <param name="targetPlatform">The target game platform.</param>
        internal static PlatformAssemblyMap GetAssemblyMap(Platform targetPlatform)
        {
            // get assembly changes needed for platform
            string[] removeAssemblyReferences;
            Assembly[] targetAssemblies;
            switch (targetPlatform)
            {
                case Platform.Mono:
                    removeAssemblyReferences = new[]
                    {
                        "Stardew Valley",
                        "Microsoft.Xna.Framework",
                        "Microsoft.Xna.Framework.Game",
                        "Microsoft.Xna.Framework.Graphics"
                    };
                    targetAssemblies = new[]
                    {
                        typeof(StardewValley.Game1).Assembly,
                        typeof(Microsoft.Xna.Framework.Vector2).Assembly
                    };
                    break;

                case Platform.Windows:
                    removeAssemblyReferences = new[]
                    {
                        "StardewValley",
                        "MonoGame.Framework"
                    };
                    targetAssemblies = new[]
                    {
                        typeof(StardewValley.Game1).Assembly,
                        typeof(Microsoft.Xna.Framework.Vector2).Assembly,
                        typeof(Microsoft.Xna.Framework.Game).Assembly,
                        typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch).Assembly
                    };
                    break;

                default:
                    throw new InvalidOperationException($"Unknown target platform '{targetPlatform}'.");
            }

            return new PlatformAssemblyMap(targetPlatform, removeAssemblyReferences, targetAssemblies);
        }

        /// <summary>Get rewriters which detect or fix incompatible CIL instructions in mod assemblies.</summary>
        internal static IEnumerable<IInstructionRewriter> GetRewriters()
        {
            return new IInstructionRewriter[]
            {
                /****
                ** Finders throw an exception when incompatible code is found.
                ****/
                // changes in Stardew Valley 1.2 (with no rewriters)
                new FieldFinder("StardewValley.Item", "set_Name"),

                // APIs removed in SMAPI 1.9
                new TypeFinder("StardewModdingAPI.Advanced.ConfigFile"),
                new TypeFinder("StardewModdingAPI.Advanced.IConfigFile"),
                new TypeFinder("StardewModdingAPI.Entities.SPlayer"),
                new TypeFinder("StardewModdingAPI.Extensions"),
                new TypeFinder("StardewModdingAPI.Inheritance.SGame"),
                new TypeFinder("StardewModdingAPI.Inheritance.SObject"),
                new TypeFinder("StardewModdingAPI.LogWriter"),
                new TypeFinder("StardewModdingAPI.Manifest"),
                new TypeFinder("StardewModdingAPI.Version"),
                new EventFinder("StardewModdingAPI.Events.GraphicsEvents", "DrawDebug"),
                new EventFinder("StardewModdingAPI.Events.GraphicsEvents", "DrawTick"),
                new EventFinder("StardewModdingAPI.Events.GraphicsEvents", "OnPostRenderHudEventNoCheck"),
                new EventFinder("StardewModdingAPI.Events.GraphicsEvents", "OnPostRenderGuiEventNoCheck"),
                new EventFinder("StardewModdingAPI.Events.GraphicsEvents", "OnPreRenderHudEventNoCheck"),
                new EventFinder("StardewModdingAPI.Events.GraphicsEvents", "OnPreRenderGuiEventNoCheck"),

                // APIs removed in SMAPI 2.0
#if !SMAPI_1_x
                new TypeFinder("StardewModdingAPI.Command"),
                new TypeFinder("StardewModdingAPI.Config"),
                new TypeFinder("StardewModdingAPI.Log"),
                new TypeFinder("StardewModdingAPI.Events.EventArgsCommand"),
                new TypeFinder("StardewModdingAPI.Events.EventArgsFarmerChanged"),
                new TypeFinder("StardewModdingAPI.Events.EventArgsLoadedGameChanged"),
                new TypeFinder("StardewModdingAPI.Events.EventArgsNewDay"),
                new TypeFinder("StardewModdingAPI.Events.EventArgsStringChanged"),
                new PropertyFinder("StardewModdingAPI.Mod", "PathOnDisk"),
                new PropertyFinder("StardewModdingAPI.Mod", "BaseConfigPath"),
                new PropertyFinder("StardewModdingAPI.Mod", "PerSaveConfigFolder"),
                new PropertyFinder("StardewModdingAPI.Mod", "PerSaveConfigPath"),
                new EventFinder("StardewModdingAPI.Events.GameEvents", "Initialize"),
                new EventFinder("StardewModdingAPI.Events.GameEvents", "LoadContent"),
                new EventFinder("StardewModdingAPI.Events.GameEvents", "GameLoaded"),
                new EventFinder("StardewModdingAPI.Events.GameEvents", "FirstUpdateTick"),
                new EventFinder("StardewModdingAPI.Events.PlayerEvents", "LoadedGame"),
                new EventFinder("StardewModdingAPI.Events.PlayerEvents", "FarmerChanged"),
                new EventFinder("StardewModdingAPI.Events.TimeEvents", "DayOfMonthChanged"),
                new EventFinder("StardewModdingAPI.Events.TimeEvents", "YearOfGameChanged"),
                new EventFinder("StardewModdingAPI.Events.TimeEvents", "SeasonOfYearChanged"),
                new EventFinder("StardewModdingAPI.Events.TimeEvents", "OnNewDay"),
#endif

                /****
                ** Rewriters change CIL as needed to fix incompatible code
                ****/
                // crossplatform
                new MethodParentRewriter(typeof(SpriteBatch), typeof(SpriteBatchWrapper), onlyIfPlatformChanged: true),

                // Stardew Valley 1.2
                new FieldToPropertyRewriter(typeof(Game1), nameof(Game1.activeClickableMenu)),
                new FieldToPropertyRewriter(typeof(Game1), nameof(Game1.currentMinigame)),
                new FieldToPropertyRewriter(typeof(Game1), nameof(Game1.gameMode)),
                new FieldToPropertyRewriter(typeof(Game1), nameof(Game1.player)),
                new FieldReplaceRewriter(typeof(Game1), "borderFont", nameof(Game1.smallFont)),
                new FieldReplaceRewriter(typeof(Game1), "smoothFont", nameof(Game1.smallFont)),

                // SMAPI 1.9
                new TypeReferenceRewriter("StardewModdingAPI.Inheritance.ItemStackChange", typeof(ItemStackChange))
            };
        }

        /// <summary>Get game current version as it should be displayed to players.</summary>
        /// <param name="version">The semantic game version.</param>
        internal static ISemanticVersion GetGameDisplayVersion(ISemanticVersion version)
        {
            switch (version.ToString())
            {
                case "1.1.1":
                    return new SemanticVersion(1, 11, 0); // The 1.1 patch was released as 1.11
                default:
                    return version;
            }
        }

        /// <summary>Get the name of a save directory for the current player.</summary>
        private static string GetSaveFolderName()
        {
            string prefix = new string(Game1.player.name.Where(char.IsLetterOrDigit).ToArray());
            return $"{prefix}_{Game1.uniqueIDForThisGame}";
        }

        /// <summary>Get the game's current semantic version.</summary>
        private static ISemanticVersion GetGameVersion()
        {
            // get raw version
            // we need reflection because it's a constant, so SMAPI's references to it are inlined at compile-time
            FieldInfo field = typeof(Game1).GetField(nameof(Game1.version), BindingFlags.Public | BindingFlags.Static);
            if (field == null)
                throw new InvalidOperationException($"The {nameof(Game1)}.{nameof(Game1.version)} field could not be found.");
            string version = (string)field.GetValue(null);

            // get semantic version
            if (version == "1.11")
                version = "1.1.1"; // The 1.1 patch was released as 1.11, which means it's out of order for semantic version checks
            return new SemanticVersion(version);
        }
    }
}
