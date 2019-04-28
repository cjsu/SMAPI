namespace StardewModdingAPI
{
    /// <summary>Encapsulates monitoring and logging for a given module.</summary>
    public interface IMonitor
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether SMAPI is aborting. Mods don't need to worry about this unless they have background tasks.</summary>
        bool IsExiting { get; }

        /// <summary>Whether verbose logging is enabled. This enables more detailed diagnostic messages than are normally needed.</summary>
        bool IsVerbose { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Log a message for the player or developer.</summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log severity level.</param>
        void Log(string message, LogLevel level = LogLevel.Debug);

        /// <summary>Log a message that only appears when <see cref="IsVerbose"/> is enabled.</summary>
        /// <param name="message">The message to log.</param>
        void VerboseLog(string message);

        /// <summary>Immediately exit the game without saving. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
        /// <param name="reason">The reason for the shutdown.</param>
        void ExitGameImmediately(string reason);
    }
}
