namespace StardewModdingAPI.Web.Framework.LogParsing.Models
{
    /// <summary>A parsed log message.</summary>
    public class LogMessage
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The local time when the log was posted.</summary>
        public string Time { get; set; }

        /// <summary>The log level.</summary>
        public LogLevel Level { get; set; }

        /// <summary>The screen ID in split-screen mode.</summary>
        public int ScreenId { get; set; }

        /// <summary>The mod name.</summary>
        public string Mod { get; set; }

        /// <summary>The log text.</summary>
        public string Text { get; set; }

        /// <summary>The number of times this message was repeated consecutively.</summary>
        public int Repeated { get; set; }

        /// <summary>The section that this log message belongs to.</summary>
        public LogSection? Section { get; set; }

        /// <summary>Whether this message is the first one of its section.</summary>
        public bool IsStartOfSection { get; set; }
    }
}
