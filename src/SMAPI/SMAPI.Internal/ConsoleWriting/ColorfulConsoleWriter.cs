using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Internal.ConsoleWriting
{
    /// <summary>Provides a wrapper for writing color-coded text to the console.</summary>
    internal class ColorfulConsoleWriter
    {
        /*********
        ** Fields
        *********/
        /// <summary>The console text color for each log level.</summary>
        private readonly IDictionary<ConsoleLogLevel, ConsoleColor> Colors;

        /// <summary>Whether the current console supports color formatting.</summary>
        private readonly bool SupportsColor;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="platform">The target platform.</param>
        /// <param name="colorScheme">The console color scheme to use.</param>
        public ColorfulConsoleWriter(Platform platform, MonitorColorScheme colorScheme)
        {
            this.SupportsColor = this.TestColorSupport();
            this.Colors = this.GetConsoleColorScheme(platform, colorScheme);
        }

        /// <summary>Write a message line to the log.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log level.</param>
        public void WriteLine(string message, ConsoleLogLevel level)
        {
            if (this.SupportsColor)
            {
                if (level == ConsoleLogLevel.Critical)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(message);
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = this.Colors[level];
                    Console.WriteLine(message);
                    Console.ResetColor();
                }
            }
            else
                Console.WriteLine(message);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Test whether the current console supports color formatting.</summary>
        private bool TestColorSupport()
        {
            try
            {
                Console.ForegroundColor = Console.ForegroundColor;
                return true;
            }
            catch (Exception)
            {
                return false; // Mono bug
            }
        }

        /// <summary>Get the color scheme to use for the current console.</summary>
        /// <param name="platform">The target platform.</param>
        /// <param name="colorScheme">The console color scheme to use.</param>
        private IDictionary<ConsoleLogLevel, ConsoleColor> GetConsoleColorScheme(Platform platform, MonitorColorScheme colorScheme)
        {
            // auto detect color scheme
            if (colorScheme == MonitorColorScheme.AutoDetect)
            {
                colorScheme = platform == Platform.Mac
                    ? MonitorColorScheme.LightBackground // MacOS doesn't provide console background color info, but it's usually white.
                    : ColorfulConsoleWriter.IsDark(Console.BackgroundColor) ? MonitorColorScheme.DarkBackground : MonitorColorScheme.LightBackground;
            }

            // get colors for scheme
            switch (colorScheme)
            {
                case MonitorColorScheme.DarkBackground:
                    return new Dictionary<ConsoleLogLevel, ConsoleColor>
                    {
                        [ConsoleLogLevel.Trace] = ConsoleColor.DarkGray,
                        [ConsoleLogLevel.Debug] = ConsoleColor.DarkGray,
                        [ConsoleLogLevel.Info] = ConsoleColor.White,
                        [ConsoleLogLevel.Warn] = ConsoleColor.Yellow,
                        [ConsoleLogLevel.Error] = ConsoleColor.Red,
                        [ConsoleLogLevel.Alert] = ConsoleColor.Magenta,
                        [ConsoleLogLevel.Success] = ConsoleColor.DarkGreen
                    };

                case MonitorColorScheme.LightBackground:
                    return new Dictionary<ConsoleLogLevel, ConsoleColor>
                    {
                        [ConsoleLogLevel.Trace] = ConsoleColor.DarkGray,
                        [ConsoleLogLevel.Debug] = ConsoleColor.DarkGray,
                        [ConsoleLogLevel.Info] = ConsoleColor.Black,
                        [ConsoleLogLevel.Warn] = ConsoleColor.DarkYellow,
                        [ConsoleLogLevel.Error] = ConsoleColor.Red,
                        [ConsoleLogLevel.Alert] = ConsoleColor.DarkMagenta,
                        [ConsoleLogLevel.Success] = ConsoleColor.DarkGreen
                    };

                default:
                    throw new NotSupportedException($"Unknown color scheme '{colorScheme}'.");
            }
        }

        /// <summary>Get whether a console color should be considered dark, which is subjectively defined as 'white looks better than black on this text'.</summary>
        /// <param name="color">The color to check.</param>
        private static bool IsDark(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Black:
                case ConsoleColor.Blue:
                case ConsoleColor.DarkBlue:
                case ConsoleColor.DarkMagenta: // Powershell
                case ConsoleColor.DarkRed:
                case ConsoleColor.Red:
                    return true;

                default:
                    return false;
            }
        }
    }
}
