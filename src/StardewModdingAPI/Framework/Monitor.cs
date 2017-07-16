using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using StardewModdingAPI.Framework.Logging;

namespace StardewModdingAPI.Framework
{
    /// <summary>Encapsulates monitoring and logic for a given module.</summary>
    internal class Monitor : IMonitor
    {
        /*********
        ** Properties
        *********/
        /// <summary>The name of the module which logs messages using this instance.</summary>
        private readonly string Source;

        /// <summary>Manages access to the console output.</summary>
        private readonly ConsoleInterceptionManager ConsoleManager;

        /// <summary>The log file to which to write messages.</summary>
        private readonly LogFileManager LogFile;

        /// <summary>The maximum length of the <see cref="LogLevel"/> values.</summary>
        private static readonly int MaxLevelLength = (from level in Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>() select level.ToString().Length).Max();

        /// <summary>The console text color for each log level.</summary>
        private static readonly IDictionary<LogLevel, ConsoleColor> Colors = Monitor.GetConsoleColorScheme();

        /// <summary>Propagates notification that SMAPI should exit.</summary>
        private readonly CancellationTokenSource ExitTokenSource;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether SMAPI is aborting. Mods don't need to worry about this unless they have background tasks.</summary>
        public bool IsExiting => this.ExitTokenSource.IsCancellationRequested;

#if !SMAPI_1_x
        /// <summary>Whether to show the full log stamps (with time/level/logger) in the console. If false, shows a simplified stamp with only the logger.</summary>
        internal bool ShowFullStampInConsole { get; set; }
#endif

        /// <summary>Whether to show trace messages in the console.</summary>
        internal bool ShowTraceInConsole { get; set; }

        /// <summary>Whether to write anything to the console. This should be disabled if no console is available.</summary>
        internal bool WriteToConsole { get; set; } = true;

        /// <summary>Whether to write anything to the log file. This should almost always be enabled.</summary>
        internal bool WriteToFile { get; set; } = true;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="source">The name of the module which logs messages using this instance.</param>
        /// <param name="consoleManager">Manages access to the console output.</param>
        /// <param name="logFile">The log file to which to write messages.</param>
        /// <param name="exitTokenSource">Propagates notification that SMAPI should exit.</param>
        public Monitor(string source, ConsoleInterceptionManager consoleManager, LogFileManager logFile, CancellationTokenSource exitTokenSource)
        {
            // validate
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("The log source cannot be empty.");

            // initialise
            this.Source = source;
            this.LogFile = logFile ?? throw new ArgumentNullException(nameof(logFile), "The log file manager cannot be null.");
            this.ConsoleManager = consoleManager;
            this.ExitTokenSource = exitTokenSource;
        }

        /// <summary>Log a message for the player or developer.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log severity level.</param>
        public void Log(string message, LogLevel level = LogLevel.Debug)
        {
            this.LogImpl(this.Source, message, level, Monitor.Colors[level]);
        }

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        /// <param name="reason">The reason for the shutdown.</param>
        public void ExitGameImmediately(string reason)
        {
            this.LogFatal($"{this.Source} requested an immediate game shutdown: {reason}");
            this.ExitTokenSource.Cancel();
        }

#if !SMAPI_1_x
        /// <summary>Write a newline to the console and log file.</summary>
        internal void Newline()
        {
            if (this.WriteToConsole)
                this.ConsoleManager.ExclusiveWriteWithoutInterception(Console.WriteLine);
            if (this.WriteToFile)
                this.LogFile.WriteLine("");
        }
#endif

#if SMAPI_1_x
        /// <summary>Log a message for the player or developer, using the specified console color.</summary>
        /// <param name="source">The name of the mod logging the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The console color.</param>
        /// <param name="level">The log level.</param>
        [Obsolete("This method is provided for backwards compatibility and otherwise should not be used. Use " + nameof(Monitor) + "." + nameof(Monitor.Log) + " instead.")]
        internal void LegacyLog(string source, string message, ConsoleColor color, LogLevel level = LogLevel.Debug)
        {
            this.LogImpl(source, message, level, color);
        }
#endif


        /*********
        ** Private methods
        *********/
        /// <summary>Log a fatal error message.</summary>
        /// <param name="message">The message to log.</param>
        private void LogFatal(string message)
        {
            this.LogImpl(this.Source, message, LogLevel.Error, ConsoleColor.White, background: ConsoleColor.Red);
        }

        /// <summary>Write a message line to the log.</summary>
        /// <param name="source">The name of the mod logging the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log level.</param>
        /// <param name="color">The console foreground color.</param>
        /// <param name="background">The console background color (or <c>null</c> to leave it as-is).</param>
        private void LogImpl(string source, string message, LogLevel level, ConsoleColor color, ConsoleColor? background = null)
        {
            // generate message
            string levelStr = level.ToString().ToUpper().PadRight(Monitor.MaxLevelLength);

            string fullMessage = $"[{DateTime.Now:HH:mm:ss} {levelStr} {source}] {message}";
#if !SMAPI_1_x
            string consoleMessage = this.ShowFullStampInConsole ? fullMessage : $"[{source}] {message}";
#else
            string consoleMessage = fullMessage;
#endif

            // write to console
            if (this.WriteToConsole && (this.ShowTraceInConsole || level != LogLevel.Trace))
            {
                this.ConsoleManager.ExclusiveWriteWithoutInterception(() =>
                {
                    if (this.ConsoleManager.SupportsColor)
                    {
                        if (background.HasValue)
                            Console.BackgroundColor = background.Value;
                        Console.ForegroundColor = color;
                        Console.WriteLine(consoleMessage);
                        Console.ResetColor();
                    }
                    else
                        Console.WriteLine(consoleMessage);
                });
            }

            // write to log file
            if (this.WriteToFile)
                this.LogFile.WriteLine(fullMessage);
        }

        /// <summary>Get the color scheme to use for the current console.</summary>
        private static IDictionary<LogLevel, ConsoleColor> GetConsoleColorScheme()
        {
#if !SMAPI_1_x
            // scheme for dark console background
            if (Monitor.IsDark(Console.BackgroundColor))
            {
#endif
                return new Dictionary<LogLevel, ConsoleColor>
                {
                    [LogLevel.Trace] = ConsoleColor.DarkGray,
                    [LogLevel.Debug] = ConsoleColor.DarkGray,
                    [LogLevel.Info] = ConsoleColor.White,
                    [LogLevel.Warn] = ConsoleColor.Yellow,
                    [LogLevel.Error] = ConsoleColor.Red,
                    [LogLevel.Alert] = ConsoleColor.Magenta
                };
#if !SMAPI_1_x
            }

            // scheme for light console background
            return new Dictionary<LogLevel, ConsoleColor>
            {
                [LogLevel.Trace] = ConsoleColor.DarkGray,
                [LogLevel.Debug] = ConsoleColor.DarkGray,
                [LogLevel.Info] = ConsoleColor.Black,
                [LogLevel.Warn] = ConsoleColor.DarkYellow,
                [LogLevel.Error] = ConsoleColor.Red,
                [LogLevel.Alert] = ConsoleColor.DarkMagenta
            };
#endif
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
                case ConsoleColor.DarkRed:
                case ConsoleColor.Red:
                    return true;

                default:
                    return false;
            }
        }
    }
}
