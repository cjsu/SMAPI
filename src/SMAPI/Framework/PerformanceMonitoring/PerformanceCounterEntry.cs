using System;

namespace StardewModdingAPI.Framework.PerformanceMonitoring
{
    /// <summary>A single performance counter entry.</summary>
    internal struct PerformanceCounterEntry
    {
        /*********
        ** Accessors
        *********/
        /// <summary>When the entry occurred.</summary>
        public DateTime EventTime { get; }

        /// <summary>The elapsed milliseconds.</summary>
        public double ElapsedMilliseconds { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="eventTime">When the entry occurred.</param>
        /// <param name="elapsedMilliseconds">The elapsed milliseconds.</param>
        public PerformanceCounterEntry(DateTime eventTime, double elapsedMilliseconds)
        {
            this.EventTime = eventTime;
            this.ElapsedMilliseconds = elapsedMilliseconds;
        }
    }
}
