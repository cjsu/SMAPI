using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using StardewModdingAPI.Framework.Events;

namespace StardewModdingAPI.Framework.PerformanceMonitoring
{
    /// <summary>Tracks performance metrics.</summary>
    internal class PerformanceMonitor
    {
        /*********
        ** Fields
        *********/
        /// <summary>The recorded alerts.</summary>
        private readonly IList<AlertEntry> Alerts = new List<AlertEntry>();

        /// <summary>The monitor for output logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The invocation stopwatch.</summary>
        private readonly Stopwatch InvocationStopwatch = new Stopwatch();

        /// <summary>The underlying performance counter collections.</summary>
        private readonly IDictionary<string, PerformanceCounterCollection> Collections = new Dictionary<string, PerformanceCounterCollection>(StringComparer.InvariantCultureIgnoreCase);


        /*********
        ** Accessors
        *********/
        /// <summary>Whether alerts are paused.</summary>
        public bool PauseAlerts { get; set; }

        /// <summary>Whether performance counter tracking is enabled.</summary>
        public bool EnableTracking { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">The monitor for output logging.</param>
        public PerformanceMonitor(IMonitor monitor)
        {
            this.Monitor = monitor;
        }

        /// <summary>Reset all performance counters in all collections.</summary>
        public void Reset()
        {
            foreach (PerformanceCounterCollection collection in this.Collections.Values)
                collection.Reset();
        }

        /// <summary>Track the invocation time for a collection.</summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="action">The action to execute and track.</param>
        public void Track(string collectionName, Action action)
        {
            if (!this.EnableTracking)
            {
                action();
                return;
            }

            PerformanceCounterCollection collection = this.GetOrCreateCollectionByName(collectionName);
            collection.BeginTrackInvocation();
            try
            {
                action();
            }
            finally
            {
                collection.EndTrackInvocation();
            }
        }

        /// <summary>Track a single performance counter invocation in a specific collection.</summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="sourceName">The name of the source.</param>
        /// <param name="action">The action to execute and track.</param>
        public void Track(string collectionName, string sourceName, Action action)
        {
            if (!this.EnableTracking)
            {
                action();
                return;
            }

            PerformanceCounterCollection collection = this.GetOrCreateCollectionByName(collectionName);
            DateTime eventTime = DateTime.UtcNow;
            this.InvocationStopwatch.Reset();
            this.InvocationStopwatch.Start();

            try
            {
                action();
            }
            finally
            {
                this.InvocationStopwatch.Stop();
                collection.Track(sourceName, new PerformanceCounterEntry(eventTime, this.InvocationStopwatch.Elapsed.TotalMilliseconds));
            }
        }

        /// <summary>Reset the performance counters for a specific collection.</summary>
        /// <param name="name">The collection name.</param>
        public void ResetCollection(string name)
        {
            if (this.Collections.TryGetValue(name, out PerformanceCounterCollection collection))
            {
                collection.ResetCallsPerSecond();
                collection.Reset();
            }
        }

        /// <summary>Reset performance counters for a specific source.</summary>
        /// <param name="name">The name of the source.</param>
        public void ResetSource(string name)
        {
            foreach (PerformanceCounterCollection performanceCounterCollection in this.Collections.Values)
                performanceCounterCollection.ResetSource(name);
        }

        /// <summary>Print any queued alerts.</summary>
        public void PrintQueuedAlerts()
        {
            if (this.Alerts.Count == 0)
                return;

            StringBuilder report = new StringBuilder();

            foreach (AlertEntry alert in this.Alerts)
            {
                report.AppendLine($"{alert.Collection.Name} took {alert.ExecutionTimeMilliseconds:F2}ms (exceeded threshold of {alert.ThresholdMilliseconds:F2}ms)");

                foreach (AlertContext context in alert.Context.OrderByDescending(p => p.Elapsed))
                    report.AppendLine(context.ToString());
            }

            this.Alerts.Clear();
            this.Monitor.Log(report.ToString(), LogLevel.Error);
        }

        /// <summary>Add an alert to the queue.</summary>
        /// <param name="entry">The alert to add.</param>
        public void AddAlert(AlertEntry entry)
        {
            if (!this.PauseAlerts)
                this.Alerts.Add(entry);
        }

        /// <summary>Initialize the default performance counter collections.</summary>
        /// <param name="eventManager">The event manager.</param>
        public void InitializePerformanceCounterCollections(EventManager eventManager)
        {
            foreach (IManagedEvent @event in eventManager.GetAllEvents())
                this.Collections[@event.EventName] = new PerformanceCounterCollection(this, @event.EventName, @event.IsPerformanceCritical);
        }

        /// <summary>Get the underlying performance counters.</summary>
        public IEnumerable<PerformanceCounterCollection> GetCollections()
        {
            return this.Collections.Values;
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Get a collection by name and creates it if it doesn't exist.</summary>
        /// <param name="name">The name of the collection.</param>
        private PerformanceCounterCollection GetOrCreateCollectionByName(string name)
        {
            if (!this.Collections.TryGetValue(name, out PerformanceCounterCollection collection))
            {
                collection = new PerformanceCounterCollection(this, name);
                this.Collections[name] = collection;
            }
            return collection;
        }
    }
}
