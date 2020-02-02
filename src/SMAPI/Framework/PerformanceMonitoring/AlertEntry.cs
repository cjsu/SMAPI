namespace StardewModdingAPI.Framework.PerformanceMonitoring
{
    /// <summary>A single alert entry.</summary>
    internal struct AlertEntry
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The collection in which the alert occurred.</summary>
        public PerformanceCounterCollection Collection { get; }

        /// <summary>The actual execution time in milliseconds.</summary>
        public double ExecutionTimeMilliseconds { get; }

        /// <summary>The configured alert threshold in milliseconds.</summary>
        public double ThresholdMilliseconds { get; }

        /// <summary>The sources involved in exceeding the threshold.</summary>
        public AlertContext[] Context { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="collection">The collection in which the alert occurred.</param>
        /// <param name="executionTimeMilliseconds">The actual execution time in milliseconds.</param>
        /// <param name="thresholdMilliseconds">The configured alert threshold in milliseconds.</param>
        /// <param name="context">The sources involved in exceeding the threshold.</param>
        public AlertEntry(PerformanceCounterCollection collection, double executionTimeMilliseconds, double thresholdMilliseconds, AlertContext[] context)
        {
            this.Collection = collection;
            this.ExecutionTimeMilliseconds = executionTimeMilliseconds;
            this.ThresholdMilliseconds = thresholdMilliseconds;
            this.Context = context;
        }
    }
}
