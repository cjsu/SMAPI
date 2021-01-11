using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if SMAPI_FOR_WINDOWS
using System.Management;
#endif
using System.Runtime.InteropServices;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Toolkit.Framework
{
    /// <summary>Provides low-level methods for fetching environment information.</summary>
    /// <remarks>This is used by the SMAPI core before the toolkit DLL is available; most code should use <see cref="EnvironmentUtility"/> instead.</remarks>
    internal static class LowLevelEnvironmentUtility
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
        public static string DetectPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return nameof(Platform.Mac);

                case PlatformID.Unix when LowLevelEnvironmentUtility.IsRunningAndroid():
                    return nameof(Platform.Android);

                case PlatformID.Unix when LowLevelEnvironmentUtility.IsRunningMac():
                    return nameof(Platform.Mac);

                case PlatformID.Unix:
                    return nameof(Platform.Linux);

                default:
                    return nameof(Platform.Windows);
            }
        }


        /// <summary>Get the human-readable OS name and version.</summary>
        /// <param name="platform">The current platform.</param>
        [SuppressMessage("ReSharper", "EmptyGeneralCatchClause", Justification = "Error suppressed deliberately to fallback to default behaviour.")]
        public static string GetFriendlyPlatformName(string platform)
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

            string name = Environment.OSVersion.ToString();
            switch (platform)
            {
                case nameof(Platform.Android):
                    name = $"Android {name}";
                    break;

                case nameof(Platform.Mac):
                    name = $"MacOS {name}";
                    break;
            }
            return name;
        }

        /// <summary>Get the name of the Stardew Valley executable.</summary>
        /// <param name="platform">The current platform.</param>
        public static string GetExecutableName(string platform)
        {
            return platform == nameof(Platform.Windows)
                ? "Stardew Valley.exe"
                : "StardewValley.exe";
        }

        /// <summary>Get whether the platform uses Mono.</summary>
        /// <param name="platform">The current platform.</param>
        public static bool IsMono(string platform)
        {
            return platform == nameof(Platform.Linux) || platform == nameof(Platform.Mac);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Detect whether the code is running on Android.</summary>
        /// <remarks>
        /// This code is derived from https://stackoverflow.com/a/47521647/262123. It detects Android by calling the
        /// <c>getprop</c> system command to check for an Android-specific property.
        /// </remarks>
        private static bool IsRunningAndroid()
        {
            using Process process = new Process
            {
                StartInfo =
                {
                    FileName = "getprop",
                    Arguments = "ro.build.user",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                return !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
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
                if (LowLevelEnvironmentUtility.uname(buffer) == 0)
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
