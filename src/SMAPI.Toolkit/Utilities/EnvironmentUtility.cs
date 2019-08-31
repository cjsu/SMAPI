using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if SMAPI_FOR_WINDOWS
using System.Management;
#endif
using System.Runtime.InteropServices;

namespace StardewModdingAPI.Toolkit.Utilities
{
    /// <summary>Provides methods for fetching environment information.</summary>
    public static class EnvironmentUtility
    {
        /*********
        ** Fields
        *********/
        /// <summary>The cached platform.</summary>
        private static Platform? CachedPlatform;

        /// <summary>Get the OS name from the system uname command.</summary>
        /// <param name="buffer">The buffer to fill with the resulting string.</param>
        [DllImport("libc")]
        static extern int uname(IntPtr buffer);


        /*********
        ** Public methods
        *********/
        /// <summary>Detect the current OS.</summary>
        public static Platform DetectPlatform()
        {
            if (EnvironmentUtility.CachedPlatform == null)
                EnvironmentUtility.CachedPlatform = EnvironmentUtility.DetectPlatformImpl();

            return EnvironmentUtility.CachedPlatform.Value;
        }


        /// <summary>Get the human-readable OS name and version.</summary>
        /// <param name="platform">The current platform.</param>
        [SuppressMessage("ReSharper", "EmptyGeneralCatchClause", Justification = "Error suppressed deliberately to fallback to default behaviour.")]
        public static string GetFriendlyPlatformName(Platform platform)
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
            return (platform == Platform.Mac ? "MacOS " : "") + Environment.OSVersion;
        }

        /// <summary>Get the name of the Stardew Valley executable.</summary>
        /// <param name="platform">The current platform.</param>
        public static string GetExecutableName(Platform platform)
        {
            return platform == Platform.Windows
                ? "Stardew Valley.exe"
                : "StardewValley.exe";
        }

        /// <summary>Get whether the platform uses Mono.</summary>
        /// <param name="platform">The current platform.</param>
        public static bool IsMono(this Platform platform)
        {
            return platform == Platform.Linux || platform == Platform.Mac;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Detect the current OS.</summary>
        private static Platform DetectPlatformImpl()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return Platform.Mac;

                case PlatformID.Unix when EnvironmentUtility.IsRunningAndroid():
                    return Platform.Android;

                case PlatformID.Unix when EnvironmentUtility.IsRunningMac():
                    return Platform.Mac;

                case PlatformID.Unix:
                    return Platform.Linux;

                default:
                    return Platform.Windows;
            }
        }

        /// <summary>Detect whether the code is running on Android.</summary>
        /// <remarks>
        /// This code is derived from https://stackoverflow.com/a/47521647/262123. It detects Android by calling the
        /// <c>getprop</c> system command to check for an Android-specific property.
        /// </remarks>
        private static bool IsRunningAndroid()
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "getprop";
                process.StartInfo.Arguments = "ro.build.user";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                try
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    return !string.IsNullOrEmpty(output);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>Detect whether the code is running on Mac.</summary>
        /// <remarks>
        /// This code is derived from the Mono project (see System.Windows.Forms/System.Windows.Forms/XplatUI.cs). It detects Mac by calling the
        /// <c>uname</c> system command and checking the response, which is always 'Darwin' for MacOS.
        /// </remarks>
        private static bool IsRunningMac()
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                buffer = Marshal.AllocHGlobal(8192);
                if (EnvironmentUtility.uname(buffer) == 0)
                {
                    string os = Marshal.PtrToStringAnsi(buffer);
                    return os == "Darwin";
                }
                return false;
            }
            catch
            {
                return false; // default to Linux
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
