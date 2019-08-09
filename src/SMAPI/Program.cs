using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
#if SMAPI_FOR_WINDOWS
#endif
using StardewModdingAPI.Framework;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI
{
    /// <summary>The main entry point for SMAPI, responsible for hooking into and launching the game.</summary>
    internal class Program
    {
        /*********
        ** Fields
        *********/
        /// <summary>The absolute path to search for SMAPI's internal DLLs.</summary>
        /// <remarks>We can't use <see cref="Constants.ExecutionPath"/> directly, since <see cref="Constants"/> depends on DLLs loaded from this folder.</remarks>
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute", Justification = "The assembly location is never null in this context.")]
        internal static readonly string DllSearchPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "smapi-internal");


        /*********
        ** Public methods
        *********/
        /// <summary>The main entry point which hooks into and launches the game.</summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += Program.CurrentDomain_AssemblyResolve;
                Program.AssertGamePresent();
                Program.AssertGameVersion();
                Program.Start(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SMAPI failed to initialize: {ex}");
                Program.PressAnyKeyToExit(true);
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Method called when assembly resolution fails, which may return a manually resolved assembly.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
        {
            try
            {
                AssemblyName name = new AssemblyName(e.Name);
                foreach (FileInfo dll in new DirectoryInfo(Program.DllSearchPath).EnumerateFiles("*.dll"))
                {
                    if (name.Name.Equals(AssemblyName.GetAssemblyName(dll.FullName).Name, StringComparison.InvariantCultureIgnoreCase))
                        return Assembly.LoadFrom(dll.FullName);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving assembly: {ex}");
                return null;
            }
        }

        /// <summary>Assert that the game is available.</summary>
        /// <remarks>This must be checked *before* any references to <see cref="Constants"/>, and this method should not reference <see cref="Constants"/> itself to avoid errors in Mono.</remarks>
        private static void AssertGamePresent()
        {
            Platform platform = EnvironmentUtility.DetectPlatform();
            string gameAssemblyName = platform == Platform.Windows ? "Stardew Valley" : "StardewValley";
            if (Type.GetType($"StardewValley.Game1, {gameAssemblyName}", throwOnError: false) == null)
            {
                Program.PrintErrorAndExit(
                    "Oops! SMAPI can't find the game. "
                    + (Assembly.GetCallingAssembly().Location.Contains(Path.Combine("internal", "Windows")) || Assembly.GetCallingAssembly().Location.Contains(Path.Combine("internal", "Mono"))
                        ? "It looks like you're running SMAPI from the download package, but you need to run the installed version instead. "
                        : "Make sure you're running StardewModdingAPI.exe in your game folder. "
                    )
                    + "See the readme.txt file for details."
                );
            }
        }

        /// <summary>Assert that the game version is within <see cref="Constants.MinimumGameVersion"/> and <see cref="Constants.MaximumGameVersion"/>.</summary>
        private static void AssertGameVersion()
        {
            // min version
            if (Constants.GameVersion.IsOlderThan(Constants.MinimumGameVersion))
            {
                ISemanticVersion suggestedApiVersion = Constants.GetCompatibleApiVersion(Constants.GameVersion);
                Program.PrintErrorAndExit(suggestedApiVersion != null
                    ? $"Oops! You're running Stardew Valley {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. You can install SMAPI {suggestedApiVersion} instead to fix this error, or update your game to the latest version."
                    : $"Oops! You're running Stardew Valley {Constants.GameVersion}, but the oldest supported version is {Constants.MinimumGameVersion}. Please update your game before using SMAPI."
                );
            }

            // max version
            else if (Constants.MaximumGameVersion != null && Constants.GameVersion.IsNewerThan(Constants.MaximumGameVersion))
                Program.PrintErrorAndExit($"Oops! You're running Stardew Valley {Constants.GameVersion}, but this version of SMAPI is only compatible up to Stardew Valley {Constants.MaximumGameVersion}. Please check for a newer version of SMAPI: https://smapi.io.");

        }

        /// <summary>Initialize SMAPI and launch the game.</summary>
        /// <param name="args">The command-line arguments.</param>
        /// <remarks>This method is separate from <see cref="Main"/> because that can't contain any references to assemblies loaded by <see cref="CurrentDomain_AssemblyResolve"/> (e.g. via <see cref="Constants"/>), or Mono will incorrectly show an assembly resolution error before assembly resolution is set up.</remarks>
        private static void Start(string[] args)
        {
            // get flags from arguments
            bool writeToConsole = !args.Contains("--no-terminal");

            // get mods path from arguments
            string modsPath = null;
            {
                int pathIndex = Array.LastIndexOf(args, "--mods-path") + 1;
                if (pathIndex >= 1 && args.Length >= pathIndex)
                {
                    modsPath = args[pathIndex];
                    if (!string.IsNullOrWhiteSpace(modsPath) && !Path.IsPathRooted(modsPath))
                        modsPath = Path.Combine(Constants.ExecutionPath, modsPath);
                }
                if (string.IsNullOrWhiteSpace(modsPath))
                    modsPath = Constants.DefaultModsPath;
            }

            // load SMAPI
            using (SCore core = new SCore(modsPath, writeToConsole))
                core.RunInteractively();
        }

        /// <summary>Write an error directly to the console and exit.</summary>
        /// <param name="message">The error message to display.</param>
        private static void PrintErrorAndExit(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
            Program.PressAnyKeyToExit(showMessage: true);
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
    }
}
