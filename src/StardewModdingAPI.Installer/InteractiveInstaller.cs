using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;
using StardewModdingApi.Installer.Enums;

namespace StardewModdingApi.Installer
{
    /// <summary>Interactively performs the install and uninstall logic.</summary>
    internal class InteractiveInstaller
    {
        /*********
        ** Properties
        *********/
        /// <summary>The default file paths where Stardew Valley can be installed.</summary>
        /// <remarks>Derived from the crossplatform mod config: https://github.com/Pathoschild/Stardew.ModBuildConfig. </remarks>
        private readonly string[] DefaultInstallPaths = {
            // Linux
            $"{Environment.GetEnvironmentVariable("HOME")}/GOG Games/Stardew Valley/game",
            $"{Environment.GetEnvironmentVariable("HOME")}/.local/share/Steam/steamapps/common/Stardew Valley",

            // Mac
            $"{Environment.GetEnvironmentVariable("HOME")}/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS",

            // Windows
            @"C:\Program Files (x86)\GalaxyClient\Games\Stardew Valley",
            @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley"
        };

        /// <summary>The directory or file paths to remove when uninstalling SMAPI, relative to the game directory.</summary>
        private readonly string[] UninstallPaths =
        {
            // common
            "StardewModdingAPI.exe",
            "StardewModdingAPI-settings.json",
            "StardewModdingAPI.AssemblyRewriters.dll",
            "steam_appid.txt",

            // Linux/Mac only
            "Mono.Cecil.dll",
            "Mono.Cecil.Rocks.dll",
            "Newtonsoft.Json.dll",
            "StardewModdingAPI",
            "StardewModdingAPI.exe.mdb",
            "System.Numerics.dll",

            // Windows only
            "StardewModdingAPI.pdb",

            // obsolete
            "Mods/.cache"
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Run the install or uninstall script.</summary>
        /// <param name="args">The command line arguments.</param>
        /// <remarks>
        /// Initialisation flow:
        ///     1. Collect information (mainly OS and install path) and validate it.
        ///     2. Ask the user whether to install or uninstall.
        /// 
        /// Uninstall logic:
        ///     1. On Linux/Mac: if a backup of the launcher exists, delete the launcher and restore the backup.
        ///     2. Delete all files and folders in the game directory matching one of the <see cref="UninstallPaths"/>.
        /// 
        /// Install flow:
        ///     1. Run the uninstall flow.
        ///     2. Copy the SMAPI files from package/Windows or package/Mono into the game directory.
        ///     3. On Linux/Mac: back up the game launcher and replace it with the SMAPI launcher. (This isn't possible on Windows, so the user needs to configure it manually.)
        ///     4. Create the 'Mods' directory.
        ///     5. Copy the bundled mods into the 'Mods' directory (deleting any existing versions).
        ///     6. Move any mods from app data into game's mods directory.
        /// </remarks>
        public void Run(string[] args)
        {
            /****
            ** collect details
            ****/
            Platform platform = this.DetectPlatform();
            DirectoryInfo packageDir = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), platform.ToString()));
            DirectoryInfo installDir = this.InteractivelyGetInstallPath(platform);
            var paths = new
            {
                executable = Path.Combine(installDir.FullName, platform == Platform.Mono ? "StardewValley.exe" : "Stardew Valley.exe"),
                unixSmapiLauncher = Path.Combine(installDir.FullName, "StardewModdingAPI"),
                unixLauncher = Path.Combine(installDir.FullName, "StardewValley"),
                unixLauncherBackup = Path.Combine(installDir.FullName, "StardewValley-original")
            };

            this.PrintDebug($"Detected {(platform == Platform.Windows ? "Windows" : "Linux or Mac")} with game in {installDir}.");

            /****
            ** validate assumptions
            ****/
            if (!packageDir.Exists)
            {
                this.ExitError($"The '{platform}' package directory is missing (should be at {packageDir}).");
                return;
            }
            if (!File.Exists(paths.executable))
            {
                this.ExitError("The detected game install path doesn't contain a Stardew Valley executable.");
                return;
            }
            Console.WriteLine();

            /****
            ** ask user what to do
            ****/
            Console.WriteLine("You can....");
            Console.WriteLine(platform == Platform.Mono
                ? "[1] Install SMAPI. This will safely update the files so you can launch the game the same way as before."
                : "[1] Install SMAPI. You'll need to launch StardewModdingAPI.exe instead afterwards; see the readme.txt for details."
            );
            Console.WriteLine("[2] Uninstall SMAPI.");

            ScriptAction action;
            {
                string choice = this.InteractivelyChoose("What do you want to do?", "1", "2");
                switch (choice)
                {
                    case "1":
                        action = ScriptAction.Install;
                        break;
                    case "2":
                        action = ScriptAction.Uninstall;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected action key '{choice}'.");
                }
            }
            Console.WriteLine();

            /****
            ** Always uninstall old files
            ****/
            // restore game launcher
            if (platform == Platform.Mono && File.Exists(paths.unixLauncherBackup))
            {
                this.PrintDebug("Removing SMAPI launcher...");
                if (File.Exists(paths.unixLauncher))
                    File.Delete(paths.unixLauncher);
                File.Move(paths.unixLauncherBackup, paths.unixLauncher);
            }

            // remove old files
            string[] removePaths = this.UninstallPaths
                .Select(path => Path.Combine(installDir.FullName, path))
                .Where(path => Directory.Exists(path) || File.Exists(path))
                .ToArray();
            if (removePaths.Any())
            {
                this.PrintDebug(action == ScriptAction.Install ? "Removing previous SMAPI files..." : "Removing SMAPI files...");
                foreach (string path in removePaths)
                {
                    if (Directory.Exists(path))
                        Directory.Delete(path, recursive: true);
                    else
                        File.Delete(path);
                }
            }

            /****
            ** Install new files
            ****/
            if (action == ScriptAction.Install)
            {
                // copy SMAPI files to game dir
                this.PrintDebug("Adding SMAPI files...");
                foreach (FileInfo sourceFile in packageDir.EnumerateFiles())
                {
                    string targetPath = Path.Combine(installDir.FullName, sourceFile.Name);
                    if (File.Exists(targetPath))
                        File.Delete(targetPath);
                    sourceFile.CopyTo(targetPath);
                }

                // replace mod launcher (if possible)
                if (platform == Platform.Mono)
                {
                    this.PrintDebug("Safely replacing game launcher...");
                    if (!File.Exists(paths.unixLauncherBackup))
                        File.Move(paths.unixLauncher, paths.unixLauncherBackup);
                    else if (File.Exists(paths.unixLauncher))
                        File.Delete(paths.unixLauncher);

                    File.Move(paths.unixSmapiLauncher, paths.unixLauncher);
                }

                // create mods directory (if needed)
                DirectoryInfo modsDir = new DirectoryInfo(Path.Combine(installDir.FullName, "Mods"));
                if (!modsDir.Exists)
                {
                    this.PrintDebug("Creating mods directory...");
                    modsDir.Create();
                }

                // add or replace bundled mods
                Directory.CreateDirectory(Path.Combine(installDir.FullName, "Mods"));
                DirectoryInfo packagedModsDir = new DirectoryInfo(Path.Combine(packageDir.FullName, "Mods"));
                if (packagedModsDir.Exists && packagedModsDir.EnumerateDirectories().Any())
                {
                    this.PrintDebug("Adding bundled mods...");
                    foreach (DirectoryInfo sourceDir in packagedModsDir.EnumerateDirectories())
                    {
                        this.PrintDebug($"   adding {sourceDir.Name}...");

                        // initialise target dir
                        DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(modsDir.FullName, sourceDir.Name));
                        if (targetDir.Exists)
                            targetDir.Delete(recursive: true);
                        targetDir.Create();

                        // copy files
                        foreach (FileInfo sourceFile in sourceDir.EnumerateFiles())
                            sourceFile.CopyTo(Path.Combine(targetDir.FullName, sourceFile.Name));
                    }
                }

                // remove obsolete appdata mods
                this.InteractivelyRemoveAppDataMods(platform, modsDir);
            }
            Console.WriteLine();

            /****
            ** exit
            ****/
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Done!");
            if (platform == Platform.Windows)
            {
                Console.WriteLine(action == ScriptAction.Install
                    ? "Don't forget to launch StardewModdingAPI.exe instead of the normal game executable. See the readme.txt for details."
                    : "If you manually changed shortcuts or Steam to launch SMAPI, don't forget to change those back."
                );
            }
            else if (action == ScriptAction.Install)
                Console.WriteLine("You can launch the game the same way as before to play with mods.");
            Console.ResetColor();
            Console.ReadKey();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Detect the game's platform.</summary>
        /// <exception cref="NotSupportedException">The platform is not supported.</exception>
        private Platform DetectPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    return Platform.Mono;

                default:
                    return Platform.Windows;
            }
        }

        /// <summary>Print a debug message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintDebug(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        /// <summary>Print a warning message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintWarning(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        /// <summary>Print an error and pause the console if needed.</summary>
        /// <param name="error">The error text.</param>
        private void ExitError(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ResetColor();
            Console.ReadLine();
        }

        /// <summary>Interactively ask the user to choose a value.</summary>
        /// <param name="message">The message to print.</param>
        /// <param name="options">The allowed options (not case sensitive).</param>
        private string InteractivelyChoose(string message, params string[] options)
        {
            while (true)
            {
                Console.WriteLine(message);
                string input = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (!options.Contains(input))
                {
                    Console.WriteLine("That's not a valid option.");
                    continue;
                }
                return input;
            }
        }

        /// <summary>Interactively locate the game's install path.</summary>
        /// <param name="platform">The current platform.</param>
        private DirectoryInfo InteractivelyGetInstallPath(Platform platform)
        {
            // try default paths
            foreach (string defaultPath in this.DefaultInstallPaths)
            {
                if (Directory.Exists(defaultPath))
                    return new DirectoryInfo(defaultPath);
            }

            if (platform == Platform.Windows)
            { 
            // Needed to get 64Keys
            RegistryKey localKey = Environment.Is64BitOperatingSystem ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64) : Registry.LocalMachine;
                
            string stardewValleyPath;
            var registry = localKey.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games\1453375253");
            if (registry != null)
            {
                stardewValleyPath = (string)registry.GetValue("PATH");
                if (!string.IsNullOrEmpty(stardewValleyPath))
                {
                        registry.Close();
                        if (Directory.Exists(stardewValleyPath))
                    {
                        return new DirectoryInfo(stardewValleyPath);
                    }
                }
            }

            registry = localKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 413150");
                if (registry != null)
                {
                    stardewValleyPath = (string)registry.GetValue("InstallLocation");
                    if (!string.IsNullOrEmpty(stardewValleyPath))
                    {
                        registry.Close();
                        if (Directory.Exists(stardewValleyPath))
                        {
                            return new DirectoryInfo(stardewValleyPath);
                        }         
                    }
                }
            }

            // ask user
            Console.WriteLine("Oops, couldn't find your Stardew Valley install path automatically. You'll need to specify where the game is installed (or install SMAPI manually).");
            while (true)
            {
                // get path from user
                Console.WriteLine("   Enter the game's full directory path (the one containing 'StardewValley.exe' or 'Stardew Valley.exe').");
                Console.Write("   > ");
                string path = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(path))
                {
                    Console.WriteLine("   You must specify a directory path to continue.");
                    continue;
                }

                // normalise on Windows
                if (platform == Platform.Windows)
                    path = path.Replace("\"", ""); // in Windows, quotes are used to escape spaces and aren't part of the file path

                // get directory
                if (File.Exists(path))
                    path = Path.GetDirectoryName(path);
                DirectoryInfo directory = new DirectoryInfo(path);

                // validate path
                if (!directory.Exists)
                {
                    Console.WriteLine("   That directory doesn't seem to exist.");
                    continue;
                }
                if (!directory.EnumerateFiles("*.exe").Any(p => p.Name == "StardewValley.exe" || p.Name == "Stardew Valley.exe"))
                {
                    Console.WriteLine("   That directory doesn't contain a Stardew Valley executable.");
                    continue;
                }

                // looks OK
                Console.WriteLine("   OK!");
                return directory;
            }
        }

        /// <summary>Interactively move mods out of the appdata directory.</summary>
        /// <param name="platform">The current platform.</param>
        /// <param name="properModsDir">The directory which should contain all mods.</param>
        private void InteractivelyRemoveAppDataMods(Platform platform, DirectoryInfo properModsDir)
        {
            // get path
            string homePath = platform == Platform.Windows
                ? Environment.GetEnvironmentVariable("APPDATA")
                : Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config");
            string appDataPath = Path.Combine(homePath, "StardewValley");
            DirectoryInfo modDir = new DirectoryInfo(Path.Combine(appDataPath, "Mods"));

            // check if migration needed
            if (!modDir.Exists)
                return;
            this.PrintDebug($"Found an obsolete mod path: {modDir.FullName}");
            this.PrintDebug("   Support for mods here was dropped in SMAPI 1.0 (it was never officially supported).");

            // move mods if no conflicts (else warn)
            foreach (FileSystemInfo entry in modDir.EnumerateFileSystemInfos())
            {
                // get type
                bool isDir = entry is DirectoryInfo;
                if (!isDir && !(entry is FileInfo))
                    continue; // should never happen

                // check paths
                string newPath = Path.Combine(properModsDir.FullName, entry.Name);
                if (isDir ? Directory.Exists(newPath) : File.Exists(newPath))
                {
                    this.PrintWarning($"   Can't move {entry.Name} because it already exists in your game's mod directory.");
                    continue;
                }

                // move into mods
                this.PrintDebug($"   Moving {entry.Name} into the game's mod directory...");
                if (isDir)
                    (entry as DirectoryInfo).MoveTo(newPath);
                else
                    (entry as FileInfo).MoveTo(newPath);
            }

            // delete if empty
            if (modDir.EnumerateFileSystemInfos().Any())
                this.PrintWarning("   You have files in this folder which couldn't be moved automatically. These will be ignored by SMAPI.");
            else
            {
                this.PrintDebug("   Deleted empty directory.");
                modDir.Delete();
            }
        }
    }
}
