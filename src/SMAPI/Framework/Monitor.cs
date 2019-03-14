using System;
using System.Linq;
using System.Threading;
using StardewModdingAPI.Framework.Logging;
using StardewModdingAPI.Internal.ConsoleWriting;

namespace StardewModdingAPI.Framework
{
    /// <summary>Encapsulates monitoring and logic for a given module.</summary>
    internal class Monitor : IMonitor
    {
        /*********
        ** Fields
        *********/
        /// <summary>The name of the module which logs messages using this instance.</summary>
        private readonly string Source;

        /// <summary>Handles writing color-coded text to the console.</summary>
        private readonly ColorfulConsoleWriter ConsoleWriter;

        /// <summary>Manages access to the console output.</summary>
        private readonly ConsoleInterceptionManager ConsoleInterceptor;

        /// <summary>The log file to which to write messages.</summary>
        private readonly LogFileManager LogFile;

        /// <summary>The maximum length of the <see cref="LogLevel"/> values.</summary>
        private static readonly int MaxLevelLength = (from level in Enum.GetValues(typeof(LogLevel)).Cast<LogLevel>() select level.ToString().Length).Max();

        /// <summary>Propagates notification that SMAPI should exit.</summary>
        private readonly CancellationTokenSource ExitTokenSource;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether SMAPI is aborting. Mods don't need to worry about this unless they have background tasks.</summary>
        public bool IsExiting => this.ExitTokenSource.IsCancellationRequested;

        /// <summary>Whether verbose logging is enabled. This enables more detailed diagnostic messages than are normally needed.</summary>
        public bool IsVerbose { get; }

        /// <summary>Whether to show the full log stamps (with time/level/logger) in the console. If false, shows a simplified stamp with only the logger.</summary>
        internal bool ShowFullStampInConsole { get; set; }

        /// <summary>Whether to show trace messages in the console.</summary>
        internal bool ShowTraceInConsole { get; set; }

        /// <summary>Whether to write anything to the console. This should be disabled if no console is available.</summary>
        internal bool WriteToConsole { get; set; } = true;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="source">The name of the module which logs messages using this instance.</param>
        /// <param name="consoleInterceptor">Intercepts access to the console output.</param>
        /// <param name="logFile">The log file to which to write messages.</param>
        /// <param name="exitTokenSource">Propagates notification that SMAPI should exit.</param>
        /// <param name="colorScheme">The console color scheme to use.</param>
        /// <param name="isVerbose">Whether verbose logging is enabled. This enables more detailed diagnostic messages than are normally needed.</param>
        public Monitor(string source, ConsoleInterceptionManager consoleInterceptor, LogFileManager logFile, CancellationTokenSource exitTokenSource, MonitorColorScheme colorScheme, bool isVerbose)
        {
            // validate
            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentException("The log source cannot be empty.");

            // initialise
            this.Source = source;
            this.LogFile = logFile ?? throw new ArgumentNullException(nameof(logFile), "The log file manager cannot be null.");
            this.ConsoleWriter = new ColorfulConsoleWriter(Constants.Platform, colorScheme);
            this.ConsoleInterceptor = consoleInterceptor;
            this.ExitTokenSource = exitTokenSource;
            this.IsVerbose = isVerbose;
        }

        /// <summary>Log a message for the player or developer.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log severity level.</param>
        public void Log(string message, LogLevel level = LogLevel.Trace)
        {
            this.LogImpl(this.Source, message, (ConsoleLogLevel)level);
        }

        /// <summary>Log a message that only appears when <see cref="IMonitor.IsVerbose"/> is enabled.</summary>
        /// <param name="message">The message to log.</param>
        public void VerboseLog(string message)
        {
            if (this.IsVerbose)
                this.Log(message, LogLevel.Trace);
        }

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        /// <param name="reason">The reason for the shutdown.</param>
        public void ExitGameImmediately(string reason)
        {
            this.LogFatal($"{this.Source} requested an immediate game shutdown: {reason}");
            this.ExitTokenSource.Cancel();
        }

        /// <summary>Write a newline to the console and log file.</summary>
        internal void Newline()
        {
            if (this.WriteToConsole)
                this.ConsoleInterceptor.ExclusiveWriteWithoutInterception(Console.WriteLine);
            this.LogFile.WriteLine("");
        }

        /// <summary>Log console input from the user.</summary>
        /// <param name="input">The user input to log.</param>
        internal void LogUserInput(string input)
        {
            // user input already appears in the console, so just need to write to file
            string prefix = this.GenerateMessagePrefix(this.Source, (ConsoleLogLevel)LogLevel.Info);
            this.LogFile.WriteLine($"{prefix} $>{input}");
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Log a fatal error message.</summary>
        /// <param name="message">The message to log.</param>
        private void LogFatal(string message)
        {
            this.LogImpl(this.Source, message, ConsoleLogLevel.Critical);
        }

        /// <summary>Write a message line to the log.</summary>
        /// <param name="source">The name of the mod logging the message.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log level.</param>
        private void LogImpl(string source, string message, ConsoleLogLevel level)
        {
            // generate message
            string prefix = this.GenerateMessagePrefix(source, level);
            string fullMessage = $"{prefix} {message}";
            string consoleMessage = this.ShowFullStampInConsole ? fullMessage : $"[{source}] {message}";

            // write to console
            if (this.WriteToConsole && (this.ShowTraceInConsole || level != ConsoleLogLevel.Trace))
            {
                this.ConsoleInterceptor.ExclusiveWriteWithoutInterception(() =>
                {
                    this.ConsoleWriter.WriteLine(consoleMessage, level);
                });
            }

            // write to log file
            this.LogFile.WriteLine(fullMessage);
        }

        /// <summary>Generate a message prefix for the current time.</summary>
        /// <param name="source">The name of the mod logging the message.</param>
        /// <param name="level">The log level.</param>
        private string GenerateMessagePrefix(string source, ConsoleLogLevel level)
        {
            string levelStr = level.ToString().ToUpper().PadRight(Monitor.MaxLevelLength);
            return $"[{DateTime.Now:HH:mm:ss} {levelStr} {source}]";
        }
    }
}
