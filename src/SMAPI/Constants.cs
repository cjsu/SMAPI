using System;
using System.IO;
using System.Linq;
using System.Reflection;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Toolkit.Framework;
using StardewModdingAPI.Toolkit.Utilities;
using StardewValley;

namespace StardewModdingAPI
{
    /// <summary>Contains constants that are accessed before the game itself has been loaded.</summary>
    /// <remarks>Most code should use <see cref="Constants"/> instead of this class directly.</remarks>
    internal static class EarlyConstants
    {
        //
        // Note: this class *must not* depend on any external DLL beyond .NET Framework itself.
        // That includes the game or SMAPI toolkit, since it's accessed before those are loaded.
        //
        // Adding an external dependency may seem to work in some cases, but will prevent SMAPI
        // from showing a human-readable error if the game isn't available. To test this, just
        // rename "Stardew Valley.exe" in the game folder; you should see an error like "Oops!
        // SMAPI can't find the game", not a technical exception.
        //

        /*********
        ** Accessors
        *********/
        /// <summary>The path to the game folder.</summary>
        public static string ExecutionPath { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>The absolute path to the folder containing SMAPI's internal files.</summary>
        public static readonly string InternalFilesPath = Path.Combine(EarlyConstants.ExecutionPath, "smapi-internal");

        /// <summary>The target game platform.</summary>
        internal static GamePlatform Platform { get; } = (GamePlatform)Enum.Parse(typeof(GamePlatform), LowLevelEnvironmentUtility.DetectPlatform());

        /// <summary>The game's assembly name.</summary>
        internal static string GameAssemblyName => EarlyConstants.Platform == GamePlatform.Windows ? "Stardew Valley" : "StardewValley";

        /// <summary>The <see cref="Context.ScreenId"/> value which should appear in the SMAPI log, if any.</summary>
        internal static int? LogScreenId { get; set; }
    }

    /// <summary>Contains SMAPI's constants and assumptions.</summary>
    public static class Constants
    {
        /*********
        ** Accessors
        *********/
        /****
        ** Public
        ****/
        /// <summary>SMAPI's current semantic version.</summary>
        public static ISemanticVersion ApiVersion { get; } = new Toolkit.SemanticVersion("3.9.1");

        /// <summary>The minimum supported version of Stardew Valley.</summary>
        public static ISemanticVersion MinimumGameVersion { get; } = new GameVersion("1.5.4");

        /// <summary>The maximum supported version of Stardew Valley.</summary>
        public static ISemanticVersion MaximumGameVersion { get; } = null;

        /// <summary>The target game platform.</summary>
        public static GamePlatform TargetPlatform { get; } = EarlyConstants.Platform;

        /// <summary>The path to the game folder.</summary>
        public static string ExecutionPath { get; } = EarlyConstants.ExecutionPath;

        /// <summary>The directory path containing Stardew Valley's app data.</summary>
        public static string DataPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");

        /// <summary>The directory path in which error logs should be stored.</summary>
        public static string LogDir { get; } = Path.Combine(Constants.DataPath, "ErrorLogs");

        /// <summary>The directory path where all saves are stored.</summary>
        public static string SavesPath { get; } = Path.Combine(Constants.DataPath, "Saves");

        /// <summary>The name of the current save folder (if save info is available, regardless of whether the save file exists yet).</summary>
        public static string SaveFolderName => Constants.GetSaveFolderName();

        /// <summary>The absolute path to the current save folder (if save info is available and the save file exists).</summary>
        public static string CurrentSavePath => Constants.GetSaveFolderPathIfExists();

        /****
        ** Internal
        ****/
        /// <summary>Whether SMAPI was compiled in debug mode.</summary>
        internal const bool IsDebugBuild =
#if DEBUG
            true;
#else
            false;
#endif

        /// <summary>The URL of the SMAPI home page.</summary>
        internal const string HomePageUrl = "https://smapi.io";

        /// <summary>The absolute path to the folder containing SMAPI's internal files.</summary>
        internal static readonly string InternalFilesPath = EarlyConstants.InternalFilesPath;

        /// <summary>The file path for the SMAPI configuration file.</summary>
        internal static string ApiConfigPath => Path.Combine(Constants.InternalFilesPath, "config.json");

        /// <summary>The file path for the overrides file for <see cref="ApiConfigPath"/>, which is applied over it.</summary>
        internal static string ApiUserConfigPath => Path.Combine(Constants.InternalFilesPath, "config.user.json");

        /// <summary>The file path for the SMAPI metadata file.</summary>
        internal static string ApiMetadataPath => Path.Combine(Constants.InternalFilesPath, "metadata.json");

        /// <summary>The filename prefix used for all SMAPI logs.</summary>
        internal static string LogNamePrefix { get; } = "SMAPI-";

        /// <summary>The filename for SMAPI's main log, excluding the <see cref="LogExtension"/>.</summary>
        internal static string LogFilename { get; } = $"{Constants.LogNamePrefix}latest";

        /// <summary>The filename extension for SMAPI log files.</summary>
        internal static string LogExtension { get; } = "txt";

        /// <summary>The file path for the log containing the previous fatal crash, if any.</summary>
        internal static string FatalCrashLog => Path.Combine(Constants.LogDir, "SMAPI-crash.txt");

        /// <summary>The file path which stores a fatal crash message for the next run.</summary>
        internal static string FatalCrashMarker => Path.Combine(Constants.InternalFilesPath, "StardewModdingAPI.crash.marker");

        /// <summary>The file path which stores the detected update version for the next run.</summary>
        internal static string UpdateMarker => Path.Combine(Constants.InternalFilesPath, "StardewModdingAPI.update.marker");

        /// <summary>The default full path to search for mods.</summary>
        internal static string DefaultModsPath { get; } = Path.Combine(Constants.ExecutionPath, "Mods");

        /// <summary>The actual full path to search for mods.</summary>
        internal static string ModsPath { get; set; }

        /// <summary>The game's current semantic version.</summary>
        internal static ISemanticVersion GameVersion { get; } = new GameVersion(Game1.version);

        /// <summary>The target game platform as a SMAPI toolkit constant.</summary>
        internal static Platform Platform { get; } = (Platform)Constants.TargetPlatform;

        /// <summary>The language code for non-translated mod assets.</summary>
        internal static LocalizedContentManager.LanguageCode DefaultLanguage { get; } = LocalizedContentManager.LanguageCode.en;


        /*********
        ** Internal methods
        *********/
        /// <summary>Get the SMAPI version to recommend for an older game version, if any.</summary>
        /// <param name="version">The game version to search.</param>
        /// <returns>Returns the compatible SMAPI version, or <c>null</c> if none was found.</returns>
        internal static ISemanticVersion GetCompatibleApiVersion(ISemanticVersion version)
        {
            // This covers all officially supported public game updates. It might seem like version
            // ranges would be better, but the given SMAPI versions may not be compatible with
            // intermediate unlisted versions (e.g. private beta updates).
            // 
            // Nonstandard versions are normalized by GameVersion (e.g. 1.07 => 1.0.7).
            switch (version.ToString())
            {
                case "1.4.1":
                case "1.4.0":
                    return new SemanticVersion("3.0.1");

                case "1.3.36":
                    return new SemanticVersion("2.11.2");

                case "1.3.33":
                case "1.3.32":
                    return new SemanticVersion("2.10.2");

                case "1.3.28":
                    return new SemanticVersion("2.7.0");

                case "1.2.33":
                case "1.2.32":
                case "1.2.31":
                case "1.2.30":
                    return new SemanticVersion("2.5.5");

                case "1.2.29":
                case "1.2.28":
                case "1.2.27":
                case "1.2.26":
                    return new SemanticVersion("1.13.1");

                case "1.1.1":
                case "1.1.0":
                    return new SemanticVersion("1.9.0");

                case "1.0.7.1":
                case "1.0.7":
                case "1.0.6":
                case "1.0.5.2":
                case "1.0.5.1":
                case "1.0.5":
                case "1.0.4":
                case "1.0.3":
                case "1.0.2":
                case "1.0.1":
                case "1.0.0":
                    return new SemanticVersion("0.40.0");

                default:
                    return null;
            }
        }

        /// <summary>Get metadata for mapping assemblies to the current platform.</summary>
        /// <param name="targetPlatform">The target game platform.</param>
        internal static PlatformAssemblyMap GetAssemblyMap(Platform targetPlatform)
        {
            // get assembly changes needed for platform
            string[] removeAssemblyReferences;
            Assembly[] targetAssemblies;
            switch (targetPlatform)
            {
                case Platform.Linux:
                case Platform.Mac:
                    removeAssemblyReferences = new[]
                    {
                        "Netcode",
                        "Stardew Valley",
                        "Microsoft.Xna.Framework",
                        "Microsoft.Xna.Framework.Game",
                        "Microsoft.Xna.Framework.Graphics",
                        "Microsoft.Xna.Framework.Xact",
                        "StardewModdingAPI.Toolkit.CoreInterfaces" // renamed in SMAPI 3.0
                    };
                    targetAssemblies = new[]
                    {
                        typeof(StardewValley.Game1).Assembly, // note: includes Netcode types on Linux/Mac
                        typeof(Microsoft.Xna.Framework.Vector2).Assembly,
                        typeof(StardewModdingAPI.IManifest).Assembly
                    };
                    break;

                case Platform.Windows:
                    removeAssemblyReferences = new[]
                    {
                        "StardewValley",
                        "MonoGame.Framework",
                        "StardewModdingAPI.Toolkit.CoreInterfaces" // renamed in SMAPI 3.0
                    };
                    targetAssemblies = new[]
                    {
                        typeof(Netcode.NetBool).Assembly,
                        typeof(StardewValley.Game1).Assembly,
                        typeof(Microsoft.Xna.Framework.Vector2).Assembly,
                        typeof(Microsoft.Xna.Framework.Game).Assembly,
                        typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch).Assembly,
                        typeof(StardewModdingAPI.IManifest).Assembly
                    };
                    break;

                default:
                    throw new InvalidOperationException($"Unknown target platform '{targetPlatform}'.");
            }

            return new PlatformAssemblyMap(targetPlatform, removeAssemblyReferences, targetAssemblies);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the name of the save folder, if any.</summary>
        private static string GetSaveFolderName()
        {
            // save not available
            if (Context.LoadStage == LoadStage.None)
                return null;

            // get basic info
            string saveName = Game1.GetSaveGameName(set_value: false);
            ulong saveID = Context.LoadStage == LoadStage.SaveParsed
                ? SaveGame.loaded.uniqueIDForThisGame
                : Game1.uniqueIDForThisGame;

            // build folder name
            return $"{new string(saveName.Where(char.IsLetterOrDigit).ToArray())}_{saveID}";
        }

        /// <summary>Get the path to the current save folder, if any.</summary>
        private static string GetSaveFolderPathIfExists()
        {
            string folderName = Constants.GetSaveFolderName();
            if (folderName == null)
                return null;

            string path = Path.Combine(Constants.SavesPath, folderName);
            return Directory.Exists(path)
                ? path
                : null;
        }
    }
}
