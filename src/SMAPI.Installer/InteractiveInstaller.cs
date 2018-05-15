using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using StardewModdingApi.Installer.Enums;
using StardewModdingAPI.Internal;
using StardewModdingAPI.Internal.ConsoleWriting;

namespace StardewModdingApi.Installer
{
    /// <summary>Interactively performs the install and uninstall logic.</summary>
    internal class InteractiveInstaller
    {
        /*********
        ** Properties
        *********/
        /// <summary>The name of the installer file in the package.</summary>
        private readonly string InstallerFileName = "install.exe";

        /// <summary>The <see cref="Environment.OSVersion"/> value that represents Windows 7.</summary>
        private readonly Version Windows7Version = new Version(6, 1);

        /// <summary>The default file paths where Stardew Valley can be installed.</summary>
        /// <param name="platform">The target platform.</param>
        /// <remarks>Derived from the crossplatform mod config: https://github.com/Pathoschild/Stardew.ModBuildConfig. </remarks>
        private IEnumerable<string> GetDefaultInstallPaths(Platform platform)
        {
            switch (platform)
            {
                case Platform.Linux:
                case Platform.Mac:
                    {
                        string home = Environment.GetEnvironmentVariable("HOME");

                        // Linux
                        yield return $"{home}/GOG Games/Stardew Valley/game";
                        yield return Directory.Exists($"{home}/.steam/steam/steamapps/common/Stardew Valley")
                            ? $"{home}/.steam/steam/steamapps/common/Stardew Valley"
                            : $"{home}/.local/share/Steam/steamapps/common/Stardew Valley";

                        // Mac
                        yield return "/Applications/Stardew Valley.app/Contents/MacOS";
                        yield return $"{home}/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS";
                    }
                    break;

                case Platform.Windows:
                    {
                        // Windows
                        yield return @"C:\Program Files (x86)\GalaxyClient\Games\Stardew Valley";
                        yield return @"C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley";

                        // Windows registry
                        IDictionary<string, string> registryKeys = new Dictionary<string, string>
                        {
                            [@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 413150"] = "InstallLocation", // Steam
                            [@"SOFTWARE\WOW6432Node\GOG.com\Games\1453375253"] = "PATH", // GOG on 64-bit Windows
                        };
                        foreach (var pair in registryKeys)
                        {
                            string path = this.GetLocalMachineRegistryValue(pair.Key, pair.Value);
                            if (!string.IsNullOrWhiteSpace(path))
                                yield return path;
                        }

                        //Trying to find a path via Steam installation path
                        string steampath = this.GetCurrentUserRegistryValue(@"Software\Valve\Steam", "SteamPath");
                        if (steampath != null)
                        {
                            yield return Path.Combine(steampath.Replace('/','\\'), @"steamapps\common\Stardew Valley");
                        }

                        //Getting latest path it it was set by user on the previous run
                        string userpath = this.GetCurrentUserRegistryValue(@"Software\SMAPI", "UserPath");
                        if (userpath != null)
                        {
                            yield return userpath;
                        }
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown platform '{platform}'.");
            }
        }

        /// <summary>Get the absolute file or folder paths to remove when uninstalling SMAPI.</summary>
        /// <param name="installDir">The folder for Stardew Valley and SMAPI.</param>
        /// <param name="modsDir">The folder for SMAPI mods.</param>
        private IEnumerable<string> GetUninstallPaths(DirectoryInfo installDir, DirectoryInfo modsDir)
        {
            string GetInstallPath(string path) => Path.Combine(installDir.FullName, path);

            // common
            yield return GetInstallPath("Mono.Cecil.dll");
            yield return GetInstallPath("Newtonsoft.Json.dll");
            yield return GetInstallPath("StardewModdingAPI.exe");
            yield return GetInstallPath("StardewModdingAPI.config.json");
            yield return GetInstallPath("StardewModdingAPI.metadata.json");
            yield return GetInstallPath("System.ValueTuple.dll");
            yield return GetInstallPath("steam_appid.txt");

            // Linux/Mac only
            yield return GetInstallPath("libgdiplus.dylib");
            yield return GetInstallPath("StardewModdingAPI");
            yield return GetInstallPath("StardewModdingAPI.exe.mdb");
            yield return GetInstallPath("System.Numerics.dll");
            yield return GetInstallPath("System.Runtime.Caching.dll");

            // Windows only
            yield return GetInstallPath("StardewModdingAPI.pdb");

            // obsolete
            yield return GetInstallPath(Path.Combine("Mods", ".cache")); // 1.3-1.4
            yield return GetInstallPath(Path.Combine("Mods", "TrainerMod")); // *–2.0 (renamed to ConsoleCommands)
            yield return GetInstallPath("Mono.Cecil.Rocks.dll"); // 1.3–1.8
            yield return GetInstallPath("StardewModdingAPI-settings.json"); // 1.0-1.4
            yield return GetInstallPath("StardewModdingAPI.AssemblyRewriters.dll"); // 1.3-2.5.5
            if (modsDir.Exists)
            {
                foreach (DirectoryInfo modDir in modsDir.EnumerateDirectories())
                    yield return Path.Combine(modDir.FullName, ".cache"); // 1.4–1.7
            }
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "ErrorLogs"); // remove old log files
        }

        /// <summary>Handles writing color-coded text to the console.</summary>
        private readonly ColorfulConsoleWriter ConsoleWriter;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public InteractiveInstaller()
        {
            this.ConsoleWriter = new ColorfulConsoleWriter(EnvironmentUtility.DetectPlatform(), MonitorColorScheme.AutoDetect);
        }

        /// <summary>Run the install or uninstall script.</summary>
        /// <param name="args">The command line arguments.</param>
        /// <remarks>
        /// Initialisation flow:
        ///     1. Collect information (mainly OS and install path) and validate it.
        ///     2. Ask the user whether to install or uninstall.
        ///
        /// Uninstall logic:
        ///     1. On Linux/Mac: if a backup of the launcher exists, delete the launcher and restore the backup.
        ///     2. Delete all files and folders in the game directory matching one of the values returned by <see cref="GetUninstallPaths"/>.
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
            ** Get platform & set window title
            ****/
            Platform platform = EnvironmentUtility.DetectPlatform();
            Console.Title = $"SMAPI {new SemanticVersionImpl(this.GetType().Assembly.GetName().Version)} installer on {platform} {EnvironmentUtility.GetFriendlyPlatformName(platform)}";
            Console.WriteLine();

#if SMAPI_FOR_WINDOWS
            if (platform == Platform.Linux || platform == Platform.Mac)
            {
                this.PrintError($"This is the installer for Windows. Run the 'install on {platform}.{(platform == Platform.Linux ? "sh" : "command")}' file instead.");
                Console.ReadLine();
                return;
            }
#endif

            /****
            ** read command-line arguments
            ****/
            // get action from CLI
            bool installArg = args.Contains("--install");
            bool uninstallArg = args.Contains("--uninstall");
            if (installArg && uninstallArg)
            {
                this.PrintError("You can't specify both --install and --uninstall command-line flags.");
                Console.ReadLine();
                return;
            }

            // get game path from CLI
            string gamePathArg = null;
            {
                int pathIndex = Array.LastIndexOf(args, "--game-path") + 1;
                if (pathIndex >= 1 && args.Length >= pathIndex)
                    gamePathArg = args[pathIndex];
            }

            /****
            ** collect details
            ****/
            // get game path
            DirectoryInfo installDir = this.InteractivelyGetInstallPath(platform, gamePathArg);
            if (installDir == null)
            {
                this.PrintError("Failed finding your game path.");
                Console.ReadLine();
                return;
            }

            // get folders
            DirectoryInfo packageDir = platform.IsMono()
                ? new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) // installer runs from internal folder on Mono
                : new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "internal", "Windows"));
            DirectoryInfo modsDir = new DirectoryInfo(Path.Combine(installDir.FullName, "Mods"));
            var paths = new
            {
                executable = Path.Combine(installDir.FullName, EnvironmentUtility.GetExecutableName(platform)),
                unixSmapiLauncher = Path.Combine(installDir.FullName, "StardewModdingAPI"),
                unixLauncher = Path.Combine(installDir.FullName, "StardewValley"),
                unixLauncherBackup = Path.Combine(installDir.FullName, "StardewValley-original")
            };

            // show output
            this.PrintInfo($"Your game folder: {installDir}.");

            /****
            ** validate assumptions
            ****/
            if (!packageDir.Exists)
            {
                this.PrintError(platform == Platform.Windows && packageDir.FullName.Contains(Path.GetTempPath()) && packageDir.FullName.Contains(".zip")
                    ? "The installer is missing some files. It looks like you're running the installer from inside the downloaded zip; make sure you unzip the downloaded file first, then run the installer from the unzipped folder."
                    : $"The 'internal/{packageDir.Name}' package folder is missing (should be at {packageDir})."
                );
                Console.ReadLine();
                return;
            }
            if (!File.Exists(paths.executable))
            {
                this.PrintError("The detected game install path doesn't contain a Stardew Valley executable.");
                Console.ReadLine();
                return;
            }

            /****
            ** validate Windows dependencies
            ****/
            if (platform == Platform.Windows)
            {
                // .NET Framework 4.5+
                if (!this.HasNetFramework45(platform))
                {
                    this.PrintError(Environment.OSVersion.Version >= this.Windows7Version
                            ? "Please install the latest version of .NET Framework before installing SMAPI." // Windows 7+
                            : "Please install .NET Framework 4.5 before installing SMAPI." // Windows Vista or earlier
                    );
                    this.PrintError("See the download page at https://www.microsoft.com/net/download/framework for details.");
                    Console.ReadLine();
                    return;
                }
                if (!this.HasXna(platform))
                {
                    this.PrintError("You don't seem to have XNA Framework installed. Please run the game at least once before installing SMAPI, so it can perform its first-time setup.");
                    Console.ReadLine();
                    return;
                }
            }

            Console.WriteLine();

            /****
            ** ask user what to do
            ****/
            ScriptAction action;

            if (installArg)
                action = ScriptAction.Install;
            else if (uninstallArg)
                action = ScriptAction.Uninstall;
            else
            {
                this.PrintInfo("You can....");
                this.PrintInfo("[1] Install SMAPI.");
                this.PrintInfo("[2] Uninstall SMAPI.");
                Console.WriteLine();

                string choice = this.InteractivelyChoose("What do you want to do? Type 1 or 2, then press enter.", "1", "2");
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
                Console.WriteLine();
            }

            /****
            ** Always uninstall old files
            ****/
            // restore game launcher
            if (platform.IsMono() && File.Exists(paths.unixLauncherBackup))
            {
                this.PrintDebug("Removing SMAPI launcher...");
                this.InteractivelyDelete(paths.unixLauncher);
                File.Move(paths.unixLauncherBackup, paths.unixLauncher);
            }

            // remove old files
            string[] removePaths = this.GetUninstallPaths(installDir, modsDir)
                .Where(path => Directory.Exists(path) || File.Exists(path))
                .ToArray();
            if (removePaths.Any())
            {
                this.PrintDebug(action == ScriptAction.Install ? "Removing previous SMAPI files..." : "Removing SMAPI files...");
                foreach (string path in removePaths)
                    this.InteractivelyDelete(path);
            }

            /****
            ** Install new files
            ****/
            if (action == ScriptAction.Install)
            {
                // copy SMAPI files to game dir
                this.PrintDebug("Adding SMAPI files...");
                foreach (FileInfo sourceFile in packageDir.EnumerateFiles().Where(this.ShouldCopyFile))
                {
                    if (sourceFile.Name == this.InstallerFileName)
                        continue;

                    string targetPath = Path.Combine(installDir.FullName, sourceFile.Name);
                    this.InteractivelyDelete(targetPath);
                    sourceFile.CopyTo(targetPath);
                }

                // replace mod launcher (if possible)
                if (platform.IsMono())
                {
                    this.PrintDebug("Safely replacing game launcher...");
                    if (File.Exists(paths.unixLauncher))
                    {
                        if (!File.Exists(paths.unixLauncherBackup))
                            File.Move(paths.unixLauncher, paths.unixLauncherBackup);
                        else
                            this.InteractivelyDelete(paths.unixLauncher);
                    }

                    File.Move(paths.unixSmapiLauncher, paths.unixLauncher);
                }

                // create mods directory (if needed)
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
                        this.InteractivelyDelete(targetDir.FullName);
                        targetDir.Create();

                        // copy files
                        foreach (FileInfo sourceFile in sourceDir.EnumerateFiles().Where(this.ShouldCopyFile))
                            sourceFile.CopyTo(Path.Combine(targetDir.FullName, sourceFile.Name));
                    }
                }

                // remove obsolete appdata mods
                this.InteractivelyRemoveAppDataMods(modsDir, packagedModsDir);
            }
            Console.WriteLine();
            Console.WriteLine();

            /****
            ** final instructions
            ****/
            if (platform == Platform.Windows)
            {
                if (action == ScriptAction.Install)
                {
                    this.PrintSuccess("SMAPI is installed! If you use Steam, set your launch options to enable achievements (see smapi.io/install):");
                    this.PrintSuccess($"    \"{Path.Combine(installDir.FullName, "StardewModdingAPI.exe")}\" %command%");
                    Console.WriteLine();
                    this.PrintSuccess("If you don't use Steam, launch StardewModdingAPI.exe in your game folder to play with mods.");
                }
                else
                    this.PrintSuccess("SMAPI is removed! If you configured Steam to launch SMAPI, don't forget to clear your launch options.");
            }
            else
            {
                this.PrintSuccess(action == ScriptAction.Install
                    ? "SMAPI is installed! Launch the game the same way as before to play with mods."
                    : "SMAPI is removed! Launch the game the same way as before to play without mods."
                );
            }

            Console.ReadKey();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the value of a key in the Windows HKLM registry.</summary>
        /// <param name="key">The full path of the registry key relative to HKLM.</param>
        /// <param name="name">The name of the value.</param>
        private string GetLocalMachineRegistryValue(string key, string name)
        {
            RegistryKey localMachine = Environment.Is64BitOperatingSystem ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64) : Registry.LocalMachine;
            RegistryKey openKey = localMachine.OpenSubKey(key);
            if (openKey == null)
                return null;
            using (openKey)
                return (string)openKey.GetValue(name);
        }
        /// <summary>Get the value of a key in the Windows HKCU registry.</summary>
        /// <param name="key">The full path of the registry key relative to HKCU.</param>
        /// <param name="name">The name of the value.</param>
        private string GetCurrentUserRegistryValue(string key, string name)
        {
            RegistryKey currentuser = Environment.Is64BitOperatingSystem ? RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64) : Registry.CurrentUser;
            RegistryKey openKey = currentuser.OpenSubKey(key);
            if (openKey == null)
                return null;
            using (openKey)
                return (string)openKey.GetValue(name);
        }

        /// <summary>Get the value of a key in the Windows HKCU registry.</summary>
        /// <param name="key">The full path of the registry key relative to HKCU.</param>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value to be set.</param>
        private void SetCurrentUserRegistryValue(string key, string name, string value)
        {
            RegistryKey currentuser = Environment.Is64BitOperatingSystem ? RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64) : Registry.CurrentUser;
            RegistryKey openKey = currentuser.OpenSubKey(key);
            if (openKey == null)
            {
                openKey = currentuser;
                var keys = key.Split('\\');
                openKey = keys.Aggregate(openKey, (current, k) => current.CreateSubKey(k));
            }
            using (openKey)
                openKey.SetValue(name, value);

        }

        /// <summary>Print a debug message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintDebug(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Debug);

        /// <summary>Print a debug message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintInfo(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Info);

        /// <summary>Print a warning message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintWarning(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Warn);

        /// <summary>Print a warning message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintError(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Error);

        /// <summary>Print a success message.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintSuccess(string text) => this.ConsoleWriter.WriteLine(text, ConsoleLogLevel.Success);

        /// <summary>Get whether the current system has .NET Framework 4.5 or later installed. This only applies on Windows.</summary>
        /// <param name="platform">The current platform.</param>
        /// <exception cref="NotSupportedException">The current platform is not Windows.</exception>
        private bool HasNetFramework45(Platform platform)
        {
            switch (platform)
            {
                case Platform.Windows:
                    using (RegistryKey versionKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                        return versionKey?.GetValue("Release") != null; // .NET Framework 4.5+

                default:
                    throw new NotSupportedException("The installed .NET Framework version can only be checked on Windows.");
            }
        }

        /// <summary>Get whether the current system has XNA Framework installed. This only applies on Windows.</summary>
        /// <param name="platform">The current platform.</param>
        /// <exception cref="NotSupportedException">The current platform is not Windows.</exception>
        private bool HasXna(Platform platform)
        {
            switch (platform)
            {
                case Platform.Windows:
                    using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\XNA\Framework"))
                        return key != null; // XNA Framework 4.0+

                default:
                    throw new NotSupportedException("The installed XNA Framework version can only be checked on Windows.");
            }
        }

        /// <summary>Interactively delete a file or folder path, and block until deletion completes.</summary>
        /// <param name="path">The file or folder path.</param>
        private void InteractivelyDelete(string path)
        {
            while (true)
            {
                try
                {
                    this.ForceDelete(Directory.Exists(path) ? new DirectoryInfo(path) : (FileSystemInfo)new FileInfo(path));
                    break;
                }
                catch (Exception ex)
                {
                    this.PrintError($"Oops! The installer couldn't delete {path}: [{ex.GetType().Name}] {ex.Message}.");
                    this.PrintError("Try rebooting your computer and then run the installer again. If that doesn't work, try deleting it yourself then press any key to retry.");
                    Console.ReadKey();
                }
            }
        }

        /// <summary>Delete a file or folder regardless of file permissions, and block until deletion completes.</summary>
        /// <param name="entry">The file or folder to reset.</param>
        private void ForceDelete(FileSystemInfo entry)
        {
            // ignore if already deleted
            entry.Refresh();
            if (!entry.Exists)
                return;

            // delete children
            if (entry is DirectoryInfo folder)
            {
                foreach (FileSystemInfo child in folder.GetFileSystemInfos())
                    this.ForceDelete(child);
            }

            // reset permissions & delete
            entry.Attributes = FileAttributes.Normal;
            entry.Delete();

            // wait for deletion to finish
            for (int i = 0; i < 10; i++)
            {
                entry.Refresh();
                if (entry.Exists)
                    Thread.Sleep(500);
            }

            // throw exception if deletion didn't happen before timeout
            entry.Refresh();
            if (entry.Exists)
                throw new IOException($"Timed out trying to delete {entry.FullName}");
        }

        /// <summary>Interactively ask the user to choose a value.</summary>
        /// <param name="message">The message to print.</param>
        /// <param name="options">The allowed options (not case sensitive).</param>
        private string InteractivelyChoose(string message, params string[] options)
        {
            while (true)
            {
                this.PrintInfo(message);
                string input = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (!options.Contains(input))
                {
                    this.PrintInfo("That's not a valid option.");
                    continue;
                }
                return input;
            }
        }

        /// <summary>Interactively locate the game install path to update.</summary>
        /// <param name="platform">The current platform.</param>
        /// <param name="specifiedPath">The path specified as a command-line argument (if any), which should override automatic path detection.</param>
        private DirectoryInfo InteractivelyGetInstallPath(Platform platform, string specifiedPath)
        {
            // get executable name
            string executableFilename = EnvironmentUtility.GetExecutableName(platform);

            // validate specified path
            if (specifiedPath != null)
            {
                var dir = new DirectoryInfo(specifiedPath);
                if (!dir.Exists)
                {
                    this.PrintError($"You specified --game-path \"{specifiedPath}\", but that folder doesn't exist.");
                    return null;
                }
                if (!dir.EnumerateFiles(executableFilename).Any())
                {
                    this.PrintError($"You specified --game-path \"{specifiedPath}\", but that folder doesn't contain the Stardew Valley executable.");
                    return null;
                }
                return dir;
            }

            // get installed paths
            DirectoryInfo[] defaultPaths = (
                                               from path in this.GetDefaultInstallPaths(platform).Distinct(StringComparer.InvariantCultureIgnoreCase)
                                               let dir = new DirectoryInfo(path)
                                               where dir.Exists && dir.EnumerateFiles(executableFilename).Any()
                                               select dir
                                           )
                .GroupBy(x => x.FullName) //adding duplicate paths check
                .Select(y => y.First())
                .ToArray();
                ;

            // choose where to install
            if (defaultPaths.Any())
            {
                // only one path
                if (defaultPaths.Length == 1)
                    return defaultPaths.First();

                // let user choose path
                Console.WriteLine();
                this.PrintInfo("Found multiple copies of the game:");
                for (int i = 0; i < defaultPaths.Length; i++)
                    this.PrintInfo($"[{i + 1}] {defaultPaths[i].FullName}");
                Console.WriteLine();

                string[] validOptions = Enumerable.Range(1, defaultPaths.Length).Select(p => p.ToString(CultureInfo.InvariantCulture)).ToArray();
                string choice = this.InteractivelyChoose("Where do you want to add/remove SMAPI? Type the number next to your choice, then press enter.", validOptions);
                int index = int.Parse(choice, CultureInfo.InvariantCulture) - 1;
                return defaultPaths[index];
            }

            // ask user
            this.PrintInfo("Oops, couldn't find the game automatically.");
            while (true)
            {
                // get path from user
                this.PrintInfo($"Type the file path to the game directory (the one containing '{executableFilename}'), then press enter.");
                string path = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(path))
                {
                    this.PrintInfo("   You must specify a directory path to continue.");
                    continue;
                }

                // normalise path
                if (platform == Platform.Windows)
                    path = path.Replace("\"", ""); // in Windows, quotes are used to escape spaces and aren't part of the file path
                if (platform == Platform.Linux || platform == Platform.Mac)
                    path = path.Replace("\\ ", " "); // in Linux/Mac, spaces in paths may be escaped if copied from the command line
                if (path.StartsWith("~/"))
                {
                    string home = Environment.GetEnvironmentVariable("HOME") ?? Environment.GetEnvironmentVariable("USERPROFILE");
                    path = Path.Combine(home, path.Substring(2));
                }

                // get directory
                if (File.Exists(path))
                    path = Path.GetDirectoryName(path);
                DirectoryInfo directory = new DirectoryInfo(path);

                // validate path
                if (!directory.Exists)
                {
                    this.PrintInfo("   That directory doesn't seem to exist.");
                    continue;
                }
                if (!directory.EnumerateFiles(executableFilename).Any())
                {
                    this.PrintInfo("   That directory doesn't contain a Stardew Valley executable.");
                    continue;
                }

                //saving into registry
                if (platform == Platform.Windows)
                {
                    this.SetCurrentUserRegistryValue(@"Software\SMAPI", "UserPath", directory.FullName);
                }

                // looks OK
                this.PrintInfo("   OK!");
                return directory;
            }
        }

        /// <summary>Interactively move mods out of the appdata directory.</summary>
        /// <param name="properModsDir">The directory which should contain all mods.</param>
        /// <param name="packagedModsDir">The installer directory containing packaged mods.</param>
        private void InteractivelyRemoveAppDataMods(DirectoryInfo properModsDir, DirectoryInfo packagedModsDir)
        {
            // get packaged mods to delete
            string[] packagedModNames = packagedModsDir.GetDirectories().Select(p => p.Name).ToArray();

            // get path
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley");
            DirectoryInfo modDir = new DirectoryInfo(Path.Combine(appDataPath, "Mods"));

            // check if migration needed
            if (!modDir.Exists)
                return;
            this.PrintDebug($"Found an obsolete mod path: {modDir.FullName}");
            this.PrintDebug("   Support for mods here was dropped in SMAPI 1.0 (it was never officially supported).");

            // move mods if no conflicts (else warn)
            foreach (FileSystemInfo entry in modDir.EnumerateFileSystemInfos().Where(this.ShouldCopyFile))
            {
                // get type
                bool isDir = entry is DirectoryInfo;
                if (!isDir && !(entry is FileInfo))
                    continue; // should never happen

                // delete packaged mods (newer version bundled into SMAPI)
                if (isDir && packagedModNames.Contains(entry.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    this.PrintDebug($"   Deleting {entry.Name} because it's bundled into SMAPI...");
                    this.InteractivelyDelete(entry.FullName);
                    continue;
                }

                // check paths
                string newPath = Path.Combine(properModsDir.FullName, entry.Name);
                if (isDir ? Directory.Exists(newPath) : File.Exists(newPath))
                {
                    this.PrintWarning($"   Can't move {entry.Name} because it already exists in your game's mod directory.");
                    continue;
                }

                // move into mods
                this.PrintDebug($"   Moving {entry.Name} into the game's mod directory...");
                this.Move(entry, newPath);
            }

            // delete if empty
            if (modDir.EnumerateFileSystemInfos().Any())
                this.PrintWarning("   You have files in this folder which couldn't be moved automatically. These will be ignored by SMAPI.");
            else
            {
                this.PrintDebug("   Deleted empty directory.");
                modDir.Delete(recursive: true);
            }
        }

        /// <summary>Move a filesystem entry to a new parent directory.</summary>
        /// <param name="entry">The filesystem entry to move.</param>
        /// <param name="newPath">The destination path.</param>
        /// <remarks>We can't use <see cref="FileInfo.MoveTo"/> or <see cref="DirectoryInfo.MoveTo"/>, because those don't work across partitions.</remarks>
        private void Move(FileSystemInfo entry, string newPath)
        {
            // file
            if (entry is FileInfo file)
            {
                file.CopyTo(newPath);
                file.Delete();
            }

            // directory
            else
            {
                Directory.CreateDirectory(newPath);

                DirectoryInfo directory = (DirectoryInfo)entry;
                foreach (FileSystemInfo child in directory.EnumerateFileSystemInfos().Where(this.ShouldCopyFile))
                    this.Move(child, Path.Combine(newPath, child.Name));

                directory.Delete(recursive: true);
            }
        }

        /// <summary>Get whether a file should be copied when moving a folder.</summary>
        /// <param name="file">The file info.</param>
        private bool ShouldCopyFile(FileSystemInfo file)
        {
            // ignore Mac symlink
            if (file is FileInfo && file.Name == "mcs")
                return false;

            return true;
        }
    }
}
