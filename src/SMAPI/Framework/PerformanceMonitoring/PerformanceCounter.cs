using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;

namespace StardewModdingAPI.Framework.PerformanceMonitoring
{
    /// <summary>Tracks metadata about a particular code event.</summary>
    internal class PerformanceCounter
    {
        /*********
        ** Fields
        *********/
        /// <summary>The size of the ring buffer.</summary>
        private readonly int MaxEntries = 16384;

        /// <summary>The collection to which this performance counter belongs.</summary>
        private readonly PerformanceCounterCollection ParentCollection;

        /// <summary>The performance counter entries.</summary>
        private readonly Stack<PerformanceCounterEntry> Entries;

        /// <summary>The entry with the highest execution time.</summary>
        private PerformanceCounterEntry? PeakPerformanceCounterEntry;


        /*********
        ** Accessors
        *********/
        /// <summary>The name of the source.</summary>
        public string Source { get; }

        /// <summary>The alert threshold in milliseconds</summary>
        public double AlertThresholdMilliseconds { get; set; }

        /// <summary>If alerting is enabled or not</summary>
        public bool EnableAlerts { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="parentCollection">The collection to which this performance counter belongs.</param>
        /// <param name="source">The name of the source.</param>
        public PerformanceCounter(PerformanceCounterCollection parentCollection, string source)
        {
            this.ParentCollection = parentCollection;
            this.Source = source;
            this.Entries = new Stack<PerformanceCounterEntry>(this.MaxEntries);
        }

        /// <summary>Add a performance counter entry to the list, update monitoring, and raise alerts if needed.</summary>
        /// <param name="entry">The entry to add.</param>
        public void Add(PerformanceCounterEntry entry)
        {
            // add entry
            if (this.Entries.Count > this.MaxEntries)
                this.Entries.Pop();
            this.Entries.Add(entry);

            // update metrics
            if (this.PeakPerformanceCounterEntry == null || entry.ElapsedMilliseconds > this.PeakPerformanceCounterEntry.Value.ElapsedMilliseconds)
                this.PeakPerformanceCounterEntry = entry;

            // raise alert
            if (this.EnableAlerts && entry.ElapsedMilliseconds > this.AlertThresholdMilliseconds)
                this.ParentCollection.AddAlert(entry.ElapsedMilliseconds, this.AlertThresholdMilliseconds, new AlertContext(this.Source, entry.ElapsedMilliseconds));
        }

        /// <summary>Clear all performance counter entries and monitoring.</summary>
        public void Reset()
        {
            this.Entries.Clear();
            this.PeakPerformanceCounterEntry = null;
        }

        /// <summary>Get the peak entry.</summary>
        public PerformanceCounterEntry? GetPeak()
        {
            return this.PeakPerformanceCounterEntry;
        }

        /// <summary>Get the entry with the highest execution time.</summary>
        /// <param name="range">The time range to search.</param>
        /// <param name="endTime">The end time for the <paramref name="range"/>, or null for the current time.</param>
        public PerformanceCounterEntry? GetPeak(TimeSpan range, DateTime? endTime = null)
        {
            endTime ??= DateTime.UtcNow;
            DateTime startTime = endTime.Value.Subtract(range);

            return this.Entries
                .Where(entry => entry.EventTime >= startTime && entry.EventTime <= endTime)
                .OrderByDescending(x => x.ElapsedMilliseconds)
                .FirstOrDefault();
        }

        /// <summary>Get the last entry added to the list.</summary>
        public PerformanceCounterEntry? GetLastEntry()
        {
            if (this.Entries.Count == 0)
                return null;

            return this.Entries.Peek();
        }

        /// <summary>Get the average over a given time span.</summary>
        /// <param name="range">The time range to search.</param>
        /// <param name="endTime">The end time for the <paramref name="range"/>, or null for the current time.</param>
        public double GetAverage(TimeSpan range, DateTime? endTime = null)
        {
            endTime ??= DateTime.UtcNow;
            DateTime startTime = endTime.Value.Subtract(range);

            double[] entries = this.Entries
                .Where(entry => entry.EventTime >= startTime && entry.EventTime <= endTime)
                .Select(p => p.ElapsedMilliseconds)
                .ToArray();

            return entries.Length > 0
                ? entries.Average()
                : 0;
        }
    }
}
