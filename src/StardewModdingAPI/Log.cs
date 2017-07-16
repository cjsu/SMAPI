#if SMAPI_1_x
using System;
using System.Threading;
using StardewModdingAPI.Framework;
using Monitor = StardewModdingAPI.Framework.Monitor;

namespace StardewModdingAPI
{
    /// <summary>A singleton which logs messages to the SMAPI console and log file.</summary>
    [Obsolete("Use " + nameof(Mod) + "." + nameof(Mod.Monitor))]
    public static class Log
    {
        /*********
        ** Properties
        *********/
        /// <summary>Manages deprecation warnings.</summary>
        private static DeprecationManager DeprecationManager;

        /// <summary>The underlying logger.</summary>
        private static Monitor Monitor;

        /// <summary>Tracks the installed mods.</summary>
        private static ModRegistry ModRegistry;


        /*********
        ** Public methods
        *********/
        /// <summary>Injects types required for backwards compatibility.</summary>
        /// <param name="deprecationManager">Manages deprecation warnings.</param>
        /// <param name="monitor">The underlying logger.</param>
        /// <param name="modRegistry">Tracks the installed mods.</param>
        internal static void Shim(DeprecationManager deprecationManager, Monitor monitor, ModRegistry modRegistry)
        {
            Log.DeprecationManager = deprecationManager;
            Log.Monitor = monitor;
            Log.ModRegistry = modRegistry;
        }

        /****
        ** Exceptions
        ****/
        /// <summary>Log an exception event.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.WarnDeprecated();
            Log.Monitor.Log($"Critical app domain exception: {e.ExceptionObject}", LogLevel.Error);
        }

        /// <summary>Log a thread exception event.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log.WarnDeprecated();
            Log.Monitor.Log($"Critical thread exception: {e.Exception}", LogLevel.Error);
        }

        /****
        ** Synchronous logging
        ****/
        /// <summary>Synchronously log a message to the console. NOTE: synchronous logging is discouraged; use asynchronous methods instead.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The message color.</param>
        public static void SyncColour(object message, ConsoleColor color)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), color);
        }

        /****
        ** Asynchronous logging
        ****/
        /// <summary>Asynchronously log a message to the console with the specified color.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="color">The message color.</param>
        public static void AsyncColour(object message, ConsoleColor color)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), color);
        }

        /// <summary>Asynchronously log a message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void Async(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Gray);
        }

        /// <summary>Asynchronously log a red message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void AsyncR(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Red);
        }

        /// <summary>Asynchronously log an orange message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void AsyncO(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.DarkYellow);
        }

        /// <summary>Asynchronously log a yellow message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void AsyncY(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Yellow);
        }

        /// <summary>Asynchronously log a green message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void AsyncG(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Green);
        }

        /// <summary>Asynchronously log a cyan message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void AsyncC(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Cyan);
        }

        /// <summary>Asynchronously log a magenta message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void AsyncM(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Magenta);
        }

        /// <summary>Asynchronously log a warning to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void Warning(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Yellow, LogLevel.Warn);
        }

        /// <summary>Asynchronously log an error to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void Error(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Red, LogLevel.Error);
        }

        /// <summary>Asynchronously log a success message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void Success(object message)
        {
            Log.WarnDeprecated();
            Log.AsyncG(message);
        }

        /// <summary>Asynchronously log an info message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void Info(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.White, LogLevel.Info);
        }

        /// <summary>Asynchronously log an info message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void Out(object message)
        {
            Log.WarnDeprecated();
            Log.Async($"[OUT] {message}");
        }

        /// <summary>Asynchronously log a debug message to the console.</summary>
        /// <param name="message">The message to log.</param>
        public static void Debug(object message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.DarkGray);
        }

        /// <summary>Asynchronously log a message to the file that's not shown in the console.</summary>
        /// <param name="message">The message to log.</param>
        internal static void LogToFile(string message)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message, ConsoleColor.DarkGray, LogLevel.Trace);
        }

        /// <summary>Obsolete.</summary>
        public static void LogValueNotSpecified()
        {
            Log.WarnDeprecated();
            Log.AsyncR("<value> must be specified");
        }

        /// <summary>Obsolete.</summary>
        public static void LogObjectValueNotSpecified()
        {
            Log.WarnDeprecated();
            Log.AsyncR("<object> and <value> must be specified");
        }

        /// <summary>Obsolete.</summary>
        public static void LogValueInvalid()
        {
            Log.WarnDeprecated();
            Log.AsyncR("<value> is invalid");
        }

        /// <summary>Obsolete.</summary>
        public static void LogObjectInvalid()
        {
            Log.WarnDeprecated();
            Log.AsyncR("<object> is invalid");
        }

        /// <summary>Obsolete.</summary>
        public static void LogValueNotInt32()
        {
            Log.WarnDeprecated();
            Log.AsyncR("<value> must be a whole number (Int32)");
        }

        /// <summary>Obsolete.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="disableLogging">Obsolete.</param>
        /// <param name="values">Obsolete.</param>
        [Obsolete("Parameter 'values' is no longer supported. Format before logging.")]
        private static void PrintLog(object message, bool disableLogging, params object[] values)
        {
            Log.WarnDeprecated();
            Log.Monitor.LegacyLog(Log.GetModName(), message?.ToString(), ConsoleColor.Gray);
        }

        /// <summary>Obsolete.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="values">Obsolete.</param>
        [Obsolete("Parameter 'values' is no longer supported. Format before logging.")]
        public static void Success(object message, params object[] values)
        {
            Log.WarnDeprecated();
            Log.Success(message);
        }

        /// <summary>Obsolete.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="values">Obsolete.</param>
        [Obsolete("Parameter 'values' is no longer supported. Format before logging.")]
        public static void Verbose(object message, params object[] values)
        {
            Log.WarnDeprecated();
            Log.Out(message);
        }

        /// <summary>Obsolete.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="values">Obsolete.</param>
        [Obsolete("Parameter 'values' is no longer supported. Format before logging.")]
        public static void Comment(object message, params object[] values)
        {
            Log.WarnDeprecated();
            Log.AsyncC(message);
        }

        /// <summary>Obsolete.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="values">Obsolete.</param>
        [Obsolete("Parameter 'values' is no longer supported. Format before logging.")]
        public static void Info(object message, params object[] values)
        {
            Log.WarnDeprecated();
            Log.Info(message);
        }

        /// <summary>Obsolete.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="values">Obsolete.</param>
        [Obsolete("Parameter 'values' is no longer supported. Format before logging.")]
        public static void Error(object message, params object[] values)
        {
            Log.WarnDeprecated();
            Log.Error(message);
        }

        /// <summary>Obsolete.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="values">Obsolete.</param>
        [Obsolete("Parameter 'values' is no longer supported. Format before logging.")]
        public static void Debug(object message, params object[] values)
        {
            Log.WarnDeprecated();
            Log.Debug(message);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Raise a deprecation warning.</summary>
        private static void WarnDeprecated()
        {
            Log.DeprecationManager.Warn($"the {nameof(Log)} class", "1.1", DeprecationLevel.PendingRemoval);
        }

        /// <summary>Get the name of the mod logging a message from the stack.</summary>
        private static string GetModName()
        {
            return Log.ModRegistry.GetModFromStack() ?? "<unknown mod>";
        }
    }
}
#endif