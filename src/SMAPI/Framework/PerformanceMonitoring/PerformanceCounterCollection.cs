using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StardewModdingAPI.Framework.PerformanceMonitoring
{
    internal class PerformanceCounterCollection
    {
        /*********
        ** Fields
        *********/
        /// <summary>The number of peak invocations to keep.</summary>
        private readonly int MaxEntries = 16384;

        /// <summary>The sources involved in exceeding alert thresholds.</summary>
        private readonly List<AlertContext> TriggeredPerformanceCounters = new List<AlertContext>();

        /// <summary>The stopwatch used to track the invocation time.</summary>
        private readonly Stopwatch InvocationStopwatch = new Stopwatch();

        /// <summary>The performance counter manager.</summary>
        private readonly PerformanceMonitor PerformanceMonitor;

        /// <summary>The time to calculate average calls per second.</summary>
        private DateTime CallsPerSecondStart = DateTime.UtcNow;

        /// <summary>The number of invocations.</summary>
        private long CallCount;

        /// <summary>The peak invocations.</summary>
        private readonly Stack<PeakEntry> PeakInvocations;


        /*********
        ** Accessors
        *********/
        /// <summary>The associated performance counters.</summary>
        public IDictionary<string, PerformanceCounter> PerformanceCounters { get; } = new Dictionary<string, PerformanceCounter>();

        /// <summary>The name of this collection.</summary>
        public string Name { get; }

        /// <summary>Whether the source is typically invoked at least once per second.</summary>
        public bool IsPerformanceCritical { get; }

        /// <summary>The alert threshold in milliseconds.</summary>
        public double AlertThresholdMilliseconds { get; set; }

        /// <summary>Whether alerts are enabled.</summary>
        public bool EnableAlerts { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="performanceMonitor">The performance counter manager.</param>
        /// <param name="name">The name of this collection.</param>
        /// <param name="isPerformanceCritical">Whether the source is typically invoked at least once per second.</param>
        public PerformanceCounterCollection(PerformanceMonitor performanceMonitor, string name, bool isPerformanceCritical = false)
        {
            this.PeakInvocations = new Stack<PeakEntry>(this.MaxEntries);
            this.Name = name;
            this.PerformanceMonitor = performanceMonitor;
            this.IsPerformanceCritical = isPerformanceCritical;
        }

        /// <summary>Track a single invocation for a named source.</summary>
        /// <param name="source">The name of the source.</param>
        /// <param name="entry">The entry.</param>
        public void Track(string source, PerformanceCounterEntry entry)
        {
            // add entry
            if (!this.PerformanceCounters.ContainsKey(source))
                this.PerformanceCounters.Add(source, new PerformanceCounter(this, source));
            this.PerformanceCounters[source].Add(entry);

            // raise alert
            if (this.EnableAlerts)
                this.TriggeredPerformanceCounters.Add(new AlertContext(source, entry.ElapsedMilliseconds));
        }

        /// <summary>Get the average execution time for all non-game internal sources in milliseconds.</summary>
        /// <param name="interval">The interval for which to get the average, relative to now</param>
        public double GetModsAverageExecutionTime(TimeSpan interval)
        {
            return this.PerformanceCounters
                .Where(entry => entry.Key != Constants.GamePerformanceCounterName)
                .Sum(entry => entry.Value.GetAverage(interval));
        }

        /// <summary>Get the overall average execution time in milliseconds.</summary>
        /// <param name="interval">The interval for which to get the average, relative to now</param>
        public double GetAverageExecutionTime(TimeSpan interval)
        {
            return this.PerformanceCounters
                .Sum(entry => entry.Value.GetAverage(interval));
        }

        /// <summary>Get the average execution time for game-internal sources in milliseconds.</summary>
        public double GetGameAverageExecutionTime(TimeSpan interval)
        {
            return this.PerformanceCounters.TryGetValue(Constants.GamePerformanceCounterName, out PerformanceCounter gameExecTime)
                ? gameExecTime.GetAverage(interval)
                : 0;
        }

        /// <summary>Get the peak execution time in milliseconds.</summary>
        /// <param name="range">The time range to search.</param>
        /// <param name="endTime">The end time for the <paramref name="range"/>, or null for the current time.</param>
        public double GetPeakExecutionTime(TimeSpan range, DateTime? endTime = null)
        {
            if (this.PeakInvocations.Count == 0)
                return 0;

            endTime ??= DateTime.UtcNow;
            DateTime startTime = endTime.Value.Subtract(range);

            return this.PeakInvocations
                .Where(entry => entry.EventTime >= startTime && entry.EventTime <= endTime)
                .OrderByDescending(x => x.ExecutionTimeMilliseconds)
                .Select(p => p.ExecutionTimeMilliseconds)
                .FirstOrDefault();
        }

        /// <summary>Start tracking the invocation of this collection.</summary>
        public void BeginTrackInvocation()
        {
            this.TriggeredPerformanceCounters.Clear();
            this.InvocationStopwatch.Reset();
            this.InvocationStopwatch.Start();

            this.CallCount++;
        }

        /// <summary>End tracking the invocation of this collection, and raise an alert if needed.</summary>
        public void EndTrackInvocation()
        {
            this.InvocationStopwatch.Stop();

            // add invocation
            if (this.PeakInvocations.Count >= this.MaxEntries)
                this.PeakInvocations.Pop();
            this.PeakInvocations.Push(new PeakEntry(this.InvocationStopwatch.Elapsed.TotalMilliseconds, DateTime.UtcNow, this.TriggeredPerformanceCounters.ToArray()));

            // raise alert
            if (this.EnableAlerts && this.InvocationStopwatch.Elapsed.TotalMilliseconds >= this.AlertThresholdMilliseconds)
                this.AddAlert(this.InvocationStopwatch.Elapsed.TotalMilliseconds, this.AlertThresholdMilliseconds, this.TriggeredPerformanceCounters.ToArray());
        }

        /// <summary>Add an alert.</summary>
        /// <param name="executionTimeMilliseconds">The execution time in milliseconds.</param>
        /// <param name="thresholdMilliseconds">The configured threshold.</param>
        /// <param name="alerts">The sources involved in exceeding the threshold.</param>
        public void AddAlert(double executionTimeMilliseconds, double thresholdMilliseconds, AlertContext[] alerts)
        {
            this.PerformanceMonitor.AddAlert(
                new AlertEntry(this, executionTimeMilliseconds, thresholdMilliseconds, alerts)
            );
        }

        /// <summary>Add an alert.</summary>
        /// <param name="executionTimeMilliseconds">The execution time in milliseconds.</param>
        /// <param name="thresholdMilliseconds">The configured threshold.</param>
        /// <param name="alert">The source involved in exceeding the threshold.</param>
        public void AddAlert(double executionTimeMilliseconds, double thresholdMilliseconds, AlertContext alert)
        {
            this.AddAlert(executionTimeMilliseconds, thresholdMilliseconds, new[] { alert });
        }

        /// <summary>Reset the calls per second counter.</summary>
        public void ResetCallsPerSecond()
        {
            this.CallCount = 0;
            this.CallsPerSecondStart = DateTime.UtcNow;
        }

        /// <summary>Reset all performance counters in this collection.</summary>
        public void Reset()
        {
            this.PeakInvocations.Clear();
            foreach (var counter in this.PerformanceCounters)
                counter.Value.Reset();
        }

        /// <summary>Reset the performance counter for a specific source.</summary>
        /// <param name="source">The source name.</param>
        public void ResetSource(string source)
        {
            foreach (var i in this.PerformanceCounters)
                if (i.Value.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase))
                    i.Value.Reset();
        }

        /// <summary>Get the average calls per second.</summary>
        public long GetAverageCallsPerSecond()
        {
            long runtimeInSeconds = (long)DateTime.UtcNow.Subtract(this.CallsPerSecondStart).TotalSeconds;
            return runtimeInSeconds > 0
                ? this.CallCount / runtimeInSeconds
                : 0;
        }
    }
}
