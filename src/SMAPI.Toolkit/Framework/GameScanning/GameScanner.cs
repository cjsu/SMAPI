using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using StardewModdingAPI.Toolkit.Utilities;
#if SMAPI_FOR_WINDOWS
using Microsoft.Win32;
#endif

namespace StardewModdingAPI.Toolkit.Framework.GameScanning
{
    /// <summary>Finds installed game folders.</summary>
    public class GameScanner
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Find all valid Stardew Valley install folders.</summary>
        /// <remarks>This checks default game locations, and on Windows checks the Windows registry for GOG/Steam install data. A folder is considered 'valid' if it contains the Stardew Valley executable for the current OS.</remarks>
        public IEnumerable<DirectoryInfo> Scan()
        {
            // get OS info
            Platform platform = EnvironmentUtility.DetectPlatform();
            string executableFilename = EnvironmentUtility.GetExecutableName(platform);

            // get install paths
            IEnumerable<string> paths = this
                .GetCustomInstallPaths(platform)
                .Concat(this.GetDefaultInstallPaths(platform))
                .Select(PathUtilities.NormalizePathSeparators)
                .Distinct(StringComparer.InvariantCultureIgnoreCase);

            // yield valid folders
            foreach (string path in paths)
            {
                DirectoryInfo folder = new DirectoryInfo(path);
                if (folder.Exists && folder.EnumerateFiles(executableFilename).Any())
                    yield return folder;
            }
        }


        /*********
        ** Private methods
        *********/
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
                        foreach (string programFiles in new[] { @"C:\Program Files", @"C:\Program Files (x86)" })
                        {
                            yield return $@"{programFiles}\GalaxyClient\Games\Stardew Valley";
                            yield return $@"{programFiles}\GOG Galaxy\Games\Stardew Valley";
                            yield return $@"{programFiles}\Steam\steamapps\common\Stardew Valley";
                        }

                        // Windows registry
#if SMAPI_FOR_WINDOWS
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

                        // via Steam library path
                        string steampath = this.GetCurrentUserRegistryValue(@"Software\Valve\Steam", "SteamPath");
                        if (steampath != null)
                            yield return Path.Combine(steampath.Replace('/', '\\'), @"steamapps\common\Stardew Valley");
#endif
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Unknown platform '{platform}'.");
            }
        }

        /// <summary>Get the custom install path from the <c>stardewvalley.targets</c> file in the home directory, if any.</summary>
        /// <param name="platform">The target platform.</param>
        private IEnumerable<string> GetCustomInstallPaths(Platform platform)
        {
            // get home path
            string homePath = Environment.GetEnvironmentVariable(platform == Platform.Windows ? "USERPROFILE" : "HOME");
            if (string.IsNullOrWhiteSpace(homePath))
                yield break;

            // get targets file
            FileInfo file = new FileInfo(Path.Combine(homePath, "stardewvalley.targets"));
            if (!file.Exists)
                yield break;

            // parse file
            XElement root;
            try
            {
                using (FileStream stream = file.OpenRead())
                    root = XElement.Load(stream);
            }
            catch
            {
                yield break;
            }

            // get install path
            XElement element = root.XPathSelectElement("//*[local-name() = 'GamePath']"); // can't use '//GamePath' due to the default namespace
            if (!string.IsNullOrWhiteSpace(element?.Value))
                yield return element.Value.Trim();
        }

#if SMAPI_FOR_WINDOWS
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
#endif
    }
}
