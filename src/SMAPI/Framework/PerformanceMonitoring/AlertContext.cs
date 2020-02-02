namespace StardewModdingAPI.Framework.PerformanceMonitoring
{
    /// <summary>The context for an alert.</summary>
    internal struct AlertContext
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The source which triggered the alert.</summary>
        public string Source { get; }

        /// <summary>The elapsed milliseconds.</summary>
        public double Elapsed { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="source">The source which triggered the alert.</param>
        /// <param name="elapsed">The elapsed milliseconds.</param>
        public AlertContext(string source, double elapsed)
        {
            this.Source = source;
            this.Elapsed = elapsed;
        }

        /// <summary>Get a human-readable text form of this instance.</summary>
        public override string ToString()
        {
            return $"{this.Source}: {this.Elapsed:F2}ms";
        }
    }
}
