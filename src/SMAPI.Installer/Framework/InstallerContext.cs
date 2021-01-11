using System;
using System.IO;
using Microsoft.Win32;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Toolkit.Framework.GameScanning;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Installer.Framework
{
    /// <summary>The installer context.</summary>
    internal class InstallerContext
    {
        /*********
        ** Fields
        *********/
        /// <summary>The <see cref="Environment.OSVersion"/> value that represents Windows 7.</summary>
        private readonly Version Windows7Version = new Version(6, 1);

        /// <summary>The underlying toolkit game scanner.</summary>
        private readonly GameScanner GameScanner = new GameScanner();


        /*********
        ** Accessors
        *********/
        /// <summary>The current OS.</summary>
        public Platform Platform { get; }

        /// <summary>The human-readable OS name and version.</summary>
        public string PlatformName { get; }

        /// <summary>The name of the Stardew Valley executable.</summary>
        public string ExecutableName { get; }

        /// <summary>Whether the installer is running on Windows.</summary>
        public bool IsWindows => this.Platform == Platform.Windows;

        /// <summary>Whether the installer is running on a Unix OS (including Linux or MacOS).</summary>
        public bool IsUnix => !this.IsWindows;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public InstallerContext()
        {
            this.Platform = EnvironmentUtility.DetectPlatform();
            this.PlatformName = EnvironmentUtility.GetFriendlyPlatformName(this.Platform);
            this.ExecutableName = EnvironmentUtility.GetExecutableName(this.Platform);
        }

        /// <summary>Get the installer's version number.</summary>
        public ISemanticVersion GetInstallerVersion()
        {
            var raw = this.GetType().Assembly.GetName().Version;
            return new SemanticVersion(raw);
        }

        /// <summary>Get whether the current system has .NET Framework 4.5 or later installed. This only applies on Windows.</summary>
        /// <exception cref="NotSupportedException">The current platform is not Windows.</exception>
        public bool HasNetFramework45()
        {
            switch (this.Platform)
            {
                case Platform.Windows:
                    using (RegistryKey versionKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                        return versionKey?.GetValue("Release") != null; // .NET Framework 4.5+

                default:
                    throw new NotSupportedException("The installed .NET Framework version can only be checked on Windows.");
            }
        }

        /// <summary>Get whether the current system has XNA Framework installed. This only applies on Windows.</summary>
        /// <exception cref="NotSupportedException">The current platform is not Windows.</exception>
        public bool HasXna()
        {
            switch (this.Platform)
            {
                case Platform.Windows:
                    using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\XNA\Framework"))
                        return key != null; // XNA Framework 4.0+

                default:
                    throw new NotSupportedException("The installed XNA Framework version can only be checked on Windows.");
            }
        }

        /// <summary>Whether the current OS supports newer versions of .NET Framework.</summary>
        public bool CanInstallLatestNetFramework()
        {
            return Environment.OSVersion.Version >= this.Windows7Version; // Windows 7+
        }

        /// <summary>Get whether a folder seems to contain the game files.</summary>
        /// <param name="dir">The folder to check.</param>
        public bool LooksLikeGameFolder(DirectoryInfo dir)
        {
            return this.GameScanner.LooksLikeGameFolder(dir);
        }
    }
}
