using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if SMAPI_FOR_WINDOWS
using System.Management;
#endif
using System.Runtime.InteropServices;

namespace StardewModdingAPI.Internal
{
    /// <summary>Provides methods for fetching environment information.</summary>
    internal static class EnvironmentUtility
    {
        /*********
        ** Fields
        *********/
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
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return Platform.Mac;

                case PlatformID.Unix:
                    return EnvironmentUtility.IsRunningMac()
                        ? Platform.Mac
                        : Platform.Linux;

                default:
                    return Platform.Windows;
            }
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
