using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using StardewModdingApi.Installer.Enums;
using StardewModdingAPI.Installer.Framework;
using StardewModdingAPI.Internal.ConsoleWriting;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.ModScanning;
using StardewModdingAPI.Toolkit.Utilities;
#if !SMAPI_FOR_WINDOWS
using System.Diagnostics;
#endif

namespace StardewModdingApi.Installer
{
    /// <summary>Interactively performs the install and uninstall logic.</summary>
    internal class InteractiveInstaller
    {
        /*********
        ** Fields
        *********/
        /// <summary>The absolute path to the directory containing the files to copy into the game folder.</summary>
        private readonly string BundlePath;

        /// <summary>The <see cref="Environment.OSVersion"/> value that represents Windows 7.</summary>
        private readonly Version Windows7Version = new Version(6, 1);

        /// <summary>The mod IDs which the installer should allow as bundled mods.</summary>
        private readonly string[] BundledModIDs = new[]
        {
            "SMAPI.SaveBackup",
            "SMAPI.ConsoleCommands"
        };



        /// <summary>Get the absolute file or folder paths to remove when uninstalling SMAPI.</summary>
        /// <param name="installDir">The folder for Stardew Valley and SMAPI.</param>
        /// <param name="modsDir">The folder for SMAPI mods.</param>
        private IEnumerable<string> GetUninstallPaths(DirectoryInfo installDir, DirectoryInfo modsDir)
        {
            string GetInstallPath(string path) => Path.Combine(installDir.FullName, path);

            // current files
            yield return GetInstallPath("libgdiplus.dylib");           // Linux/Mac only
            yield return GetInstallPath("StardewModdingAPI");          // Linux/Mac only
            yield return GetInstallPath("StardewModdingAPI.exe");
            yield return GetInstallPath("StardewModdingAPI.exe.config");
            yield return GetInstallPath("StardewModdingAPI.exe.mdb");  // Linux/Mac only
            yield return GetInstallPath("StardewModdingAPI.pdb");      // Windows only
            yield return GetInstallPath("StardewModdingAPI.xml");
            yield return GetInstallPath("smapi-internal");
            yield return GetInstallPath("steam_appid.txt");

            // obsolete
            yield return GetInstallPath(Path.Combine("Mods", ".cache"));     // 1.3-1.4
            yield return GetInstallPath(Path.Combine("Mods", "TrainerMod")); // *–2.0 (renamed to ConsoleCommands)
            yield return GetInstallPath("Mono.Cecil.Rocks.dll");             // 1.3–1.8
            yield return GetInstallPath("StardewModdingAPI-settings.json");  // 1.0-1.4
            yield return GetInstallPath("StardewModdingAPI.AssemblyRewriters.dll"); // 1.3-2.5.5
            yield return GetInstallPath("0Harmony.dll");                    // moved in 2.8
            yield return GetInstallPath("0Harmony.pdb");                    // moved in 2.8
            yield return GetInstallPath("Mono.Cecil.dll");                  // moved in 2.8
            yield return GetInstallPath("Newtonsoft.Json.dll");             // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.config.json");   // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.crash.marker");  // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.metadata.json"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.update.marker"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.dll");   // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.pdb");   // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.xml");   // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.CoreInterfaces.dll"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.CoreInterfaces.pdb"); // moved in 2.8
            yield return GetInstallPath("StardewModdingAPI.Toolkit.CoreInterfaces.xml"); // moved in 2.8
            yield return GetInstallPath("System.Numerics.dll");               // moved in 2.8
            yield return GetInstallPath("System.Runtime.Caching.dll");        // moved in 2.8
            yield return GetInstallPath("System.ValueTuple.dll");             // moved in 2.8

            if (modsDir.Exists)
            {
                foreach (DirectoryInfo modDir in modsDir.EnumerateDirectories())
                    yield return Path.Combine(modDir.FullName, ".cache"); // 1.4–1.7
            }
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "ErrorLogs"); // remove old log files
        }

        /// <summary>Handles writing color-coded text to the console.</summary>
        private ColorfulConsoleWriter ConsoleWriter;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="bundlePath">The absolute path to the directory containing the files to copy into the game folder.</param>
        public InteractiveInstaller(string bundlePath)
        {
            this.BundlePath = bundlePath;
            this.ConsoleWriter = new ColorfulConsoleWriter(EnvironmentUtility.DetectPlatform());
        }

        /// <summary>Run the install or uninstall script.</summary>
        /// <param name="args">The command line arguments.</param>
        /// <remarks>
        /// Initialization flow:
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
            /*********
            ** Step 1: initial setup
            *********/
            /****
            ** Get basic info & set window title
            ****/
            ModToolkit toolkit = new ModToolkit();
            Platform platform = EnvironmentUtility.DetectPlatform();
            Console.Title = $"SMAPI {this.GetDisplayVersion(this.GetType().Assembly.GetName().Version)} installer on {platform} {EnvironmentUtility.GetFriendlyPlatformName(platform)}";
            Console.WriteLine();

            /****
            ** Check if correct installer
            ****/
#if SMAPI_FOR_WINDOWS
            if (platform == Platform.Linux || platform == Platform.Mac)
            {
                this.PrintError($"This is the installer for Windows. Run the 'install on {platform}.{(platform == Platform.Linux ? "sh" : "command")}' file instead.");
                Console.ReadLine();
                return;
            }
#else
            if (platform == Platform.Windows)
            {
                this.PrintError($"This is the installer for Linux/Mac. Run the 'install on Windows.exe' file instead.");
                Console.ReadLine();
                return;
            }
#endif

            /****
            ** Check Windows dependencies
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


            /*********
            ** Step 2: choose a theme (can't auto-detect on Linux/Mac)
            *********/
            MonitorColorScheme scheme = MonitorColorScheme.AutoDetect;
            if (platform == Platform.Linux || platform == Platform.Mac)
            {
                /****
                ** print header
                ****/
                this.PrintPlain("Hi there! I'll help you install or remove SMAPI. Just a few questions first.");
                this.PrintPlain("----------------------------------------------------------------------------");
                Console.WriteLine();

                /****
                ** show theme selector
                ****/
                // get theme writers
                var lightBackgroundWriter = new ColorfulConsoleWriter(platform, ColorfulConsoleWriter.GetDefaultColorSchemeConfig(MonitorColorScheme.LightBackground));
                var darkBackgroundWriter = new ColorfulConsoleWriter(platform, ColorfulConsoleWriter.GetDefaultColorSchemeConfig(MonitorColorScheme.DarkBackground));

                // print question
                this.PrintPlain("Which text looks more readable?");
                Console.WriteLine();
                Console.Write("   [1] ");
                lightBackgroundWriter.WriteLine("Dark text on light background", ConsoleLogLevel.Info);
                Console.Write("   [2] ");
                darkBackgroundWriter.WriteLine("Light text on dark background", ConsoleLogLevel.Info);
                Console.WriteLine();

                // handle choice
                string choice = this.InteractivelyChoose("Type 1 or 2, then press enter.", new[] { "1", "2" });
                switch (choice)
                {
                    case "1":
                        scheme = MonitorColorScheme.LightBackground;
                        this.ConsoleWriter = lightBackgroundWriter;
                        break;
                    case "2":
                        scheme = MonitorColorScheme.DarkBackground;
                        this.ConsoleWriter = darkBackgroundWriter;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected action key '{choice}'.");
                }
            }
            Console.Clear();


            /*********
            ** Step 3: find game folder
            *********/
            InstallerPaths paths;
            {
                /****
                ** print header
                ****/
                this.PrintInfo("Hi there! I'll help you install or remove SMAPI. Just a few questions first.");
                this.PrintDebug($"Color scheme: {this.GetDisplayText(scheme)}");
                this.PrintDebug("----------------------------------------------------------------------------");
                Console.WriteLine();

                /****
                ** collect details
                ****/
                // get game path
                this.PrintInfo("Where is your game folder?");
                DirectoryInfo installDir = this.InteractivelyGetInstallPath(platform, toolkit, gamePathArg);
                if (installDir == null)
                {
                    this.PrintError("Failed finding your game path.");
                    Console.ReadLine();
                    return;
                }

                // get folders
                DirectoryInfo bundleDir = new DirectoryInfo(this.BundlePath);
                paths = new InstallerPaths(bundleDir, installDir, EnvironmentUtility.GetExecutableName(platform));
            }
            Console.Clear();


            /*********
            ** Step 4: validate assumptions
            *********/
            if (!File.Exists(paths.ExecutablePath))
            {
                this.PrintError("The detected game install path doesn't contain a Stardew Valley executable.");
                Console.ReadLine();
                return;
            }


            /*********
            ** Step 5: ask what to do
            *********/
            ScriptAction action;
            {
                /****
                ** print header
                ****/
                this.PrintInfo("Hi there! I'll help you install or remove SMAPI. Just one question first.");
                this.PrintDebug($"Game path: {paths.GamePath}");
                this.PrintDebug($"Color scheme: {this.GetDisplayText(scheme)}");
                this.PrintDebug("----------------------------------------------------------------------------");
                Console.WriteLine();

                /****
                ** ask what to do
                ****/
                if (installArg)
                    action = ScriptAction.Install;
                else if (uninstallArg)
                    action = ScriptAction.Uninstall;
                else
                {
                    this.PrintInfo("What do you want to do?");
                    Console.WriteLine();
                    this.PrintInfo("[1] Install SMAPI.");
                    this.PrintInfo("[2] Uninstall SMAPI.");
                    Console.WriteLine();

                    string choice = this.InteractivelyChoose("Type 1 or 2, then press enter.", new[] { "1", "2" });
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
            }
            Console.Clear();


            /*********
            ** Step 6: apply
            *********/
            {
                /****
                ** print header
                ****/
                this.PrintInfo($"That's all I need! I'll {action.ToString().ToLower()} SMAPI now.");
                this.PrintDebug($"Game path: {paths.GamePath}");
                this.PrintDebug($"Color scheme: {this.GetDisplayText(scheme)}");
                this.PrintDebug("----------------------------------------------------------------------------");
                Console.WriteLine();

                /****
                ** Always uninstall old files
                ****/
                // restore game launcher
                if (platform.IsMono() && File.Exists(paths.UnixBackupLauncherPath))
                {
                    this.PrintDebug("Removing SMAPI launcher...");
                    this.InteractivelyDelete(paths.UnixLauncherPath);
                    File.Move(paths.UnixBackupLauncherPath, paths.UnixLauncherPath);
                }

                // remove old files
                string[] removePaths = this.GetUninstallPaths(paths.GameDir, paths.ModsDir)
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
                    foreach (FileSystemInfo sourceEntry in paths.BundleDir.EnumerateFileSystemInfos().Where(this.ShouldCopy))
                    {
                        this.InteractivelyDelete(Path.Combine(paths.GameDir.FullName, sourceEntry.Name));
                        this.RecursiveCopy(sourceEntry, paths.GameDir);
                    }

                    // replace mod launcher (if possible)
                    if (platform.IsMono())
                    {
                        this.PrintDebug("Safely replacing game launcher...");

                        // back up & remove current launcher
                        if (File.Exists(paths.UnixLauncherPath))
                        {
                            if (!File.Exists(paths.UnixBackupLauncherPath))
                                File.Move(paths.UnixLauncherPath, paths.UnixBackupLauncherPath);
                            else
                                this.InteractivelyDelete(paths.UnixLauncherPath);
                        }

                        // add new launcher
                        File.Move(paths.UnixSmapiLauncherPath, paths.UnixLauncherPath);

                        // mark file executable
                        // (MSBuild doesn't keep permission flags for files zipped in a build task.)
                        // (Note: exclude from Windows build because antivirus apps can flag the process start code as suspicious.)
#if !SMAPI_FOR_WINDOWS
                        new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "chmod",
                                Arguments = $"755 \"{paths.UnixLauncherPath}\"",
                                CreateNoWindow = true
                            }
                        }.Start();
#endif
                    }

                    // create mods directory (if needed)
                    if (!paths.ModsDir.Exists)
                    {
                        this.PrintDebug("Creating mods directory...");
                        paths.ModsDir.Create();
                    }

                    // add or replace bundled mods
                    DirectoryInfo bundledModsDir = new DirectoryInfo(Path.Combine(paths.BundlePath, "Mods"));
                    if (bundledModsDir.Exists && bundledModsDir.EnumerateDirectories().Any())
                    {
                        this.PrintDebug("Adding bundled mods...");

                        ModFolder[] targetMods = toolkit.GetModFolders(paths.ModsPath).ToArray();
                        foreach (ModFolder sourceMod in toolkit.GetModFolders(bundledModsDir.FullName))
                        {
                            // validate source mod
                            if (sourceMod.Manifest == null)
                            {
                                this.PrintWarning($"   ignored invalid bundled mod {sourceMod.DisplayName}: {sourceMod.ManifestParseError}");
                                continue;
                            }
                            if (!this.BundledModIDs.Contains(sourceMod.Manifest.UniqueID))
                            {
                                this.PrintWarning($"   ignored unknown '{sourceMod.DisplayName}' mod in the installer folder. To add mods, put them here instead: {paths.ModsPath}");
                                continue;
                            }

                            // find target folder
                            ModFolder targetMod = targetMods.FirstOrDefault(p => p.Manifest?.UniqueID?.Equals(sourceMod.Manifest.UniqueID, StringComparison.InvariantCultureIgnoreCase) == true);
                            DirectoryInfo defaultTargetFolder = new DirectoryInfo(Path.Combine(paths.ModsPath, sourceMod.Directory.Name));
                            DirectoryInfo targetFolder = targetMod?.Directory ?? defaultTargetFolder;
                            this.PrintDebug(targetFolder.FullName == defaultTargetFolder.FullName
                                ? $"   adding {sourceMod.Manifest.Name}..."
                                : $"   adding {sourceMod.Manifest.Name} to {Path.Combine(paths.ModsDir.Name, PathUtilities.GetRelativePath(paths.ModsPath, targetFolder.FullName))}..."
                            );

                            // remove existing folder
                            if (targetFolder.Exists)
                                this.InteractivelyDelete(targetFolder.FullName);

                            // copy files
                            this.RecursiveCopy(sourceMod.Directory, paths.ModsDir, filter: this.ShouldCopy);
                        }
                    }

                    // set SMAPI's color scheme if defined
                    if (scheme != MonitorColorScheme.AutoDetect)
                    {
                        string text = File
                            .ReadAllText(paths.ApiConfigPath)
                            .Replace(@"""UseScheme"": ""AutoDetect""", $@"""UseScheme"": ""{scheme}""");
                        File.WriteAllText(paths.ApiConfigPath, text);
                    }

                    // remove obsolete appdata mods
                    this.InteractivelyRemoveAppDataMods(paths.ModsDir, bundledModsDir);
                }
            }
            Console.WriteLine();
            Console.WriteLine();


            /*********
            ** Step 7: final instructions
            *********/
            if (platform == Platform.Windows)
            {
                if (action == ScriptAction.Install)
                {
                    this.PrintSuccess("SMAPI is installed! If you use Steam, set your launch options to enable achievements (see smapi.io/install):");
                    this.PrintSuccess($"    \"{Path.Combine(paths.GamePath, "StardewModdingAPI.exe")}\" %command%");
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
        /// <summary>Get the display text for an assembly version.</summary>
        /// <param name="version">The assembly version.</param>
        private string GetDisplayVersion(Version version)
        {
            string str = $"{version.Major}.{version.Minor}";
            if (version.Build != 0)
                str += $".{version.Build}";
            return str;
        }

        /// <summary>Get the display text for a color scheme.</summary>
        /// <param name="scheme">The color scheme.</param>
        private string GetDisplayText(MonitorColorScheme scheme)
        {
            switch (scheme)
            {
                case MonitorColorScheme.AutoDetect:
                    return "auto-detect";
                case MonitorColorScheme.DarkBackground:
                    return "light text on dark background";
                case MonitorColorScheme.LightBackground:
                    return "dark text on light background";
                default:
                    return scheme.ToString();
            }
        }

        /// <summary>Print a message without formatting.</summary>
        /// <param name="text">The text to print.</param>
        private void PrintPlain(string text) => Console.WriteLine(text);

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

        /// <summary>Recursively copy a directory or file.</summary>
        /// <param name="source">The file or folder to copy.</param>
        /// <param name="targetFolder">The folder to copy into.</param>
        /// <param name="filter">A filter which matches directories and files to copy, or <c>null</c> to match all.</param>
        private void RecursiveCopy(FileSystemInfo source, DirectoryInfo targetFolder, Func<FileSystemInfo, bool> filter = null)
        {
            if (filter != null && !filter(source))
                return;

            if (!targetFolder.Exists)
                targetFolder.Create();

            switch (source)
            {
                case FileInfo sourceFile:
                    sourceFile.CopyTo(Path.Combine(targetFolder.FullName, sourceFile.Name));
                    break;

                case DirectoryInfo sourceDir:
                    DirectoryInfo targetSubfolder = new DirectoryInfo(Path.Combine(targetFolder.FullName, sourceDir.Name));
                    foreach (var entry in sourceDir.EnumerateFileSystemInfos())
                        this.RecursiveCopy(entry, targetSubfolder, filter);
                    break;

                default:
                    throw new NotSupportedException($"Unknown filesystem info type '{source.GetType().FullName}'.");
            }
        }

        /// <summary>Delete a file or folder regardless of file permissions, and block until deletion completes.</summary>
        /// <param name="entry">The file or folder to reset.</param>
        /// <remarks>This method is mirrored from <c>FileUtilities.ForceDelete</c> in the toolkit.</remarks>
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
        /// <param name="print">A callback which prints a message to the console.</param>
        /// <param name="message">The message to print.</param>
        /// <param name="options">The allowed options (not case sensitive).</param>
        /// <param name="indent">The indentation to prefix to output.</param>
        private string InteractivelyChoose(string message, string[] options, string indent = "", Action<string> print = null)
        {
            print = print ?? this.PrintInfo;

            while (true)
            {
                print(indent + message);
                Console.Write(indent);
                string input = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (!options.Contains(input))
                {
                    print($"{indent}That's not a valid option.");
                    continue;
                }
                return input;
            }
        }

        /// <summary>Interactively locate the game install path to update.</summary>
        /// <param name="platform">The current platform.</param>
        /// <param name="toolkit">The mod toolkit.</param>
        /// <param name="specifiedPath">The path specified as a command-line argument (if any), which should override automatic path detection.</param>
        private DirectoryInfo InteractivelyGetInstallPath(Platform platform, ModToolkit toolkit, string specifiedPath)
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
            DirectoryInfo[] defaultPaths = toolkit.GetGameFolders().ToArray();
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

                // normalize path
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
            foreach (FileSystemInfo entry in modDir.EnumerateFileSystemInfos().Where(this.ShouldCopy))
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
                foreach (FileSystemInfo child in directory.EnumerateFileSystemInfos().Where(this.ShouldCopy))
                    this.Move(child, Path.Combine(newPath, child.Name));

                directory.Delete(recursive: true);
            }
        }

        /// <summary>Get whether a file or folder should be copied from the installer files.</summary>
        /// <param name="entry">The file or folder info.</param>
        private bool ShouldCopy(FileSystemInfo entry)
        {
            switch (entry.Name)
            {
                case "mcs":
                    return false; // ignore Mac symlink
                case "Mods":
                    return false; // Mods folder handled separately
                default:
                    return true;
            }
        }
    }
}
