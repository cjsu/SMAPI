using System;

namespace StardewModdingAPI.Framework.PerformanceMonitoring
{
    /// <summary>A peak invocation time.</summary>
    internal struct PeakEntry
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The actual execution time in milliseconds.</summary>
        public double ExecutionTimeMilliseconds { get; }

        /// <summary>When the entry occurred.</summary>
        public DateTime EventTime { get; }

        /// <summary>The sources involved in exceeding the threshold.</summary>
        public AlertContext[] Context { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="executionTimeMilliseconds">The actual execution time in milliseconds.</param>
        /// <param name="eventTime">When the entry occurred.</param>
        /// <param name="context">The sources involved in exceeding the threshold.</param>
        public PeakEntry(double executionTimeMilliseconds, DateTime eventTime, AlertContext[] context)
        {
            this.ExecutionTimeMilliseconds = executionTimeMilliseconds;
            this.EventTime = eventTime;
            this.Context = context;
        }
    }
}
