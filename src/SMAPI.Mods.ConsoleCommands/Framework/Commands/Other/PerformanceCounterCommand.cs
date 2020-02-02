using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.PerformanceMonitoring;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Other
{
    /// <summary>A set of commands which displays or configures performance monitoring.</summary>
    internal class PerformanceCounterCommand : TrainerCommand
    {
        /*********
        ** Fields
        *********/
        /// <summary>The name of the command.</summary>
        private const string CommandName = "performance";

        /// <summary>The available commands.</summary>
        private enum SubCommand
        {
            Summary,
            Detail,
            Reset,
            Trigger,
            Enable,
            Disable,
            Help
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public PerformanceCounterCommand()
            : base(CommandName, PerformanceCounterCommand.GetDescription()) { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // parse args
            SubCommand subcommand = SubCommand.Summary;
            {
                if (args.TryGet(0, "command", out string subcommandStr, false) && !Enum.TryParse(subcommandStr, ignoreCase: true, out subcommand))
                {
                    this.LogUsageError(monitor, $"Unknown command {subcommandStr}");
                    return;
                }
            }

            // handle
            switch (subcommand)
            {
                case SubCommand.Summary:
                    this.HandleSummarySubCommand(monitor, args);
                    break;

                case SubCommand.Detail:
                    this.HandleDetailSubCommand(monitor, args);
                    break;

                case SubCommand.Reset:
                    this.HandleResetSubCommand(monitor, args);
                    break;

                case SubCommand.Trigger:
                    this.HandleTriggerSubCommand(monitor, args);
                    break;

                case SubCommand.Enable:
                    SCore.PerformanceMonitor.EnableTracking = true;
                    monitor.Log("Performance counter tracking is now enabled", LogLevel.Info);
                    break;

                case SubCommand.Disable:
                    SCore.PerformanceMonitor.EnableTracking = false;
                    monitor.Log("Performance counter tracking is now disabled", LogLevel.Info);
                    break;

                case SubCommand.Help:
                    this.OutputHelp(monitor, args.TryGet(1, "command", out _) ? subcommand : null as SubCommand?);
                    break;

                default:
                    this.LogUsageError(monitor, $"Unknown command {subcommand}");
                    break;
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Handles the summary sub command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="args">The command arguments.</param>
        private void HandleSummarySubCommand(IMonitor monitor, ArgumentParser args)
        {
            if (!this.AssertEnabled(monitor))
                return;

            IEnumerable<PerformanceCounterCollection> data = SCore.PerformanceMonitor.GetCollections();

            double? threshold = null;
            if (args.TryGetDecimal(1, "threshold", out decimal t, required: false))
                threshold = (double?)t;

            TimeSpan interval = TimeSpan.FromSeconds(60);

            StringBuilder report = new StringBuilder();
            report.AppendLine($"Summary over the last {interval.TotalSeconds} seconds:");
            report.AppendLine(this.GetTableString(
                data: data,
                header: new[] { "Collection", "Avg Calls/s", "Avg Exec Time (Game)", "Avg Exec Time (Mods)", "Avg Exec Time (Game+Mods)", "Peak Exec Time" },
                getRow: item => new[]
                {
                    item.Name,
                    item.GetAverageCallsPerSecond().ToString(),
                    this.FormatMilliseconds(item.GetGameAverageExecutionTime(interval), threshold),
                    this.FormatMilliseconds(item.GetModsAverageExecutionTime(interval), threshold),
                    this.FormatMilliseconds(item.GetAverageExecutionTime(interval), threshold),
                    this.FormatMilliseconds(item.GetPeakExecutionTime(interval), threshold)
                },
                true
            ));

            monitor.Log(report.ToString(), LogLevel.Info);
        }

        /// <summary>Handles the detail sub command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="args">The command arguments.</param>
        private void HandleDetailSubCommand(IMonitor monitor, ArgumentParser args)
        {
            if (!this.AssertEnabled(monitor))
                return;

            // parse args
            double thresholdMilliseconds = 0;
            if (args.TryGetDecimal(1, "threshold", out decimal t, required: false))
                thresholdMilliseconds = (double)t;

            // get collections
            var collections = SCore.PerformanceMonitor.GetCollections();

            // render
            TimeSpan averageInterval = TimeSpan.FromSeconds(60);
            StringBuilder report = new StringBuilder($"Showing details for performance counters of {thresholdMilliseconds}+ milliseconds:\n\n");
            bool anyShown = false;
            foreach (PerformanceCounterCollection collection in collections)
            {
                KeyValuePair<string, PerformanceCounter>[] data = collection.PerformanceCounters
                    .Where(p => p.Value.GetAverage(averageInterval) >= thresholdMilliseconds)
                    .ToArray();

                if (data.Any())
                {
                    anyShown = true;
                    report.AppendLine($"{collection.Name}:");
                    report.AppendLine(this.GetTableString(
                        data: data,
                        header: new[] { "Mod", $"Avg Exec Time (last {(int)averageInterval.TotalSeconds}s)", "Last Exec Time", "Peak Exec Time", $"Peak Exec Time (last {(int)averageInterval.TotalSeconds}s)" },
                        getRow: item => new[]
                        {
                            item.Key,
                            this.FormatMilliseconds(item.Value.GetAverage(averageInterval), thresholdMilliseconds),
                            this.FormatMilliseconds(item.Value.GetLastEntry()?.ElapsedMilliseconds),
                            this.FormatMilliseconds(item.Value.GetPeak()?.ElapsedMilliseconds),
                            this.FormatMilliseconds(item.Value.GetPeak(averageInterval)?.ElapsedMilliseconds)
                        },
                        true
                    ));
                }
            }

            if (!anyShown)
                report.AppendLine("No performance counters found.");

            monitor.Log(report.ToString(), LogLevel.Info);
        }

        /// <summary>Handles the trigger sub command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="args">The command arguments.</param>
        private void HandleTriggerSubCommand(IMonitor monitor, ArgumentParser args)
        {
            if (!this.AssertEnabled(monitor))
                return;

            if (args.TryGet(1, "mode", out string mode, false))
            {
                switch (mode)
                {
                    case "list":
                        this.OutputAlertTriggers(monitor);
                        break;

                    case "collection":
                        if (args.TryGet(2, "name", out string collectionName))
                        {
                            if (args.TryGetDecimal(3, "threshold", out decimal threshold))
                            {
                                if (!args.TryGet(4, "source", out string source, required: false))
                                    source = null;
                                this.ConfigureAlertTrigger(monitor, collectionName, source, threshold);
                            }
                        }
                        break;

                    case "pause":
                        SCore.PerformanceMonitor.PauseAlerts = true;
                        monitor.Log("Alerts are now paused.", LogLevel.Info);
                        break;

                    case "resume":
                        SCore.PerformanceMonitor.PauseAlerts = false;
                        monitor.Log("Alerts are now resumed.", LogLevel.Info);
                        break;

                    case "dump":
                        this.OutputAlertTriggers(monitor, true);
                        break;

                    case "clear":
                        this.ClearAlertTriggers(monitor);
                        break;

                    default:
                        this.LogUsageError(monitor, $"Unknown mode {mode}. See '{CommandName} help trigger' for usage.");
                        break;
                }
            }
            else
                this.OutputAlertTriggers(monitor);
        }

        /// <summary>Sets up an an alert trigger.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="sourceName">The name of the source, or null for all sources.</param>
        /// <param name="threshold">The trigger threshold, or 0 to remove.</param>
        private void ConfigureAlertTrigger(IMonitor monitor, string collectionName, string sourceName, decimal threshold)
        {
            foreach (PerformanceCounterCollection collection in SCore.PerformanceMonitor.GetCollections())
            {
                if (collection.Name.ToLowerInvariant().Equals(collectionName.ToLowerInvariant()))
                {
                    if (sourceName == null)
                    {
                        if (threshold != 0)
                        {
                            collection.EnableAlerts = true;
                            collection.AlertThresholdMilliseconds = (double)threshold;
                            monitor.Log($"Set up alert triggering for '{collectionName}' with '{this.FormatMilliseconds((double?)threshold)}'", LogLevel.Info);
                        }
                        else
                        {
                            collection.EnableAlerts = false;
                            monitor.Log($"Cleared alert triggering for '{collection}'.");
                        }

                        return;
                    }
                    else
                    {
                        foreach (var performanceCounter in collection.PerformanceCounters)
                        {
                            if (performanceCounter.Value.Source.ToLowerInvariant().Equals(sourceName.ToLowerInvariant()))
                            {
                                if (threshold != 0)
                                {
                                    performanceCounter.Value.EnableAlerts = true;
                                    performanceCounter.Value.AlertThresholdMilliseconds = (double)threshold;
                                    monitor.Log($"Set up alert triggering for '{sourceName}' in collection '{collectionName}' with '{this.FormatMilliseconds((double?)threshold)}", LogLevel.Info);
                                }
                                else
                                    performanceCounter.Value.EnableAlerts = false;
                                return;
                            }
                        }

                        monitor.Log($"Could not find the source '{sourceName}' in collection '{collectionName}'", LogLevel.Warn);
                        return;
                    }
                }
            }

            monitor.Log($"Could not find the collection '{collectionName}'", LogLevel.Warn);
        }


        /// <summary>Clears alert triggering for all collections.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        private void ClearAlertTriggers(IMonitor monitor)
        {
            int clearedTriggers = 0;
            foreach (PerformanceCounterCollection collection in SCore.PerformanceMonitor.GetCollections())
            {
                if (collection.EnableAlerts)
                {
                    collection.EnableAlerts = false;
                    clearedTriggers++;
                }

                foreach (var performanceCounter in collection.PerformanceCounters)
                {
                    if (performanceCounter.Value.EnableAlerts)
                    {
                        performanceCounter.Value.EnableAlerts = false;
                        clearedTriggers++;
                    }
                }

            }

            monitor.Log($"Cleared {clearedTriggers} alert triggers.", LogLevel.Info);
        }

        /// <summary>Lists all configured alert triggers.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="asDump">True to dump the triggers as commands.</param>
        private void OutputAlertTriggers(IMonitor monitor, bool asDump = false)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("Configured triggers:");
            report.AppendLine();
            var collectionTriggers = new List<CollectionTrigger>();
            var sourceTriggers = new List<SourceTrigger>();

            foreach (PerformanceCounterCollection collection in SCore.PerformanceMonitor.GetCollections())
            {
                if (collection.EnableAlerts)
                    collectionTriggers.Add(new CollectionTrigger(collection.Name, collection.AlertThresholdMilliseconds));

                sourceTriggers.AddRange(
                    from counter in collection.PerformanceCounters
                    where counter.Value.EnableAlerts
                    select new SourceTrigger(collection.Name, counter.Value.Source, counter.Value.AlertThresholdMilliseconds)
                );
            }

            if (collectionTriggers.Count > 0)
            {
                report.AppendLine("Collection Triggers:");
                report.AppendLine();

                if (asDump)
                {
                    foreach (var item in collectionTriggers)
                        report.AppendLine($"{CommandName} trigger {item.CollectionName} {item.Threshold}");
                }
                else
                {
                    report.AppendLine(this.GetTableString(
                        data: collectionTriggers,
                        header: new[] { "Collection", "Threshold" },
                        getRow: item => new[] { item.CollectionName, this.FormatMilliseconds(item.Threshold) },
                        true
                    ));
                }

                report.AppendLine();
            }
            else
                report.AppendLine("No collection triggers.");

            if (sourceTriggers.Count > 0)
            {
                report.AppendLine("Source Triggers:");
                report.AppendLine();

                if (asDump)
                {
                    foreach (SourceTrigger item in sourceTriggers)
                        report.AppendLine($"{CommandName} trigger {item.CollectionName} {item.Threshold} {item.SourceName}");
                }
                else
                {
                    report.AppendLine(this.GetTableString(
                        data: sourceTriggers,
                        header: new[] { "Collection", "Source", "Threshold" },
                        getRow: item => new[] { item.CollectionName, item.SourceName, this.FormatMilliseconds(item.Threshold) },
                        true
                    ));
                }

                report.AppendLine();
            }
            else
                report.AppendLine("No source triggers.");

            monitor.Log(report.ToString(), LogLevel.Info);
        }

        /// <summary>Handles the reset sub command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="args">The command arguments.</param>
        private void HandleResetSubCommand(IMonitor monitor, ArgumentParser args)
        {
            if (!this.AssertEnabled(monitor))
                return;

            if (args.TryGet(1, "type", out string type, false, new[] { "category", "source" }))
            {
                args.TryGet(2, "name", out string name);

                switch (type)
                {
                    case "category":
                        SCore.PerformanceMonitor.ResetCollection(name);
                        monitor.Log($"All performance counters for category {name} are now cleared.", LogLevel.Info);
                        break;
                    case "source":
                        SCore.PerformanceMonitor.ResetSource(name);
                        monitor.Log($"All performance counters for source {name} are now cleared.", LogLevel.Info);
                        break;
                }
            }
            else
            {
                SCore.PerformanceMonitor.Reset();
                monitor.Log("All performance counters are now cleared.", LogLevel.Info);
            }
        }

        /// <summary>Formats the given milliseconds value into a string format. Optionally
        /// allows a threshold to return "-" if the value is less than the threshold.</summary>
        /// <param name="milliseconds">The milliseconds to format. Returns "-" if null</param>
        /// <param name="thresholdMilliseconds">The threshold. Any value below this is returned as "-".</param>
        /// <returns>The formatted milliseconds.</returns>
        private string FormatMilliseconds(double? milliseconds, double? thresholdMilliseconds = null)
        {
            thresholdMilliseconds ??= 1;
            return milliseconds != null && milliseconds >= thresholdMilliseconds
                   ? ((double)milliseconds).ToString("F2")
                   : "-";
        }

        /// <summary>Shows detailed help for a specific sub command.</summary>
        /// <param name="monitor">The output monitor.</param>
        /// <param name="subcommand">The subcommand.</param>
        private void OutputHelp(IMonitor monitor, SubCommand? subcommand)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine();

            switch (subcommand)
            {
                case SubCommand.Detail:
                    report.AppendLine($"       {CommandName} detail <threshold>");
                    report.AppendLine();
                    report.AppendLine("Displays details for a specific collection.");
                    report.AppendLine();
                    report.AppendLine("Arguments:");
                    report.AppendLine("  <threshold>   Optional. The threshold in milliseconds. Any average execution time below that");
                    report.AppendLine("                threshold is not reported.");
                    report.AppendLine();
                    report.AppendLine("Examples:");
                    report.AppendLine($"{CommandName} detail 5     Show counters exceeding an average of 5ms");
                    break;

                case SubCommand.Summary:
                    report.AppendLine($"Usage: {CommandName} summary <threshold>");
                    report.AppendLine();
                    report.AppendLine("Displays the performance counter summary.");
                    report.AppendLine();
                    report.AppendLine("Arguments:");
                    report.AppendLine("  <threshold> Optional. Hides the actual execution time if it's below this threshold");
                    report.AppendLine();
                    report.AppendLine("Examples:");
                    report.AppendLine($"{CommandName} summary       Show all events");
                    report.AppendLine($"{CommandName} summary 5     Shows events exceeding an average of 5ms");
                    break;

                case SubCommand.Trigger:
                    report.AppendLine($"Usage: {CommandName} trigger <mode>");
                    report.AppendLine($"Usage: {CommandName} trigger collection <collectionName> <threshold>");
                    report.AppendLine($"Usage: {CommandName} trigger collection <collectionName> <threshold> <sourceName>");
                    report.AppendLine();
                    report.AppendLine("Manages alert triggers.");
                    report.AppendLine();
                    report.AppendLine("Arguments:");
                    report.AppendLine("  <mode>           Optional. Specifies if a specific source or a specific collection should be triggered.");
                    report.AppendLine("                   - list        Lists current triggers");
                    report.AppendLine("                   - collection  Sets up a trigger for a collection");
                    report.AppendLine("                   - clear       Clears all trigger entries");
                    report.AppendLine("                   - pause       Pauses triggering of alerts");
                    report.AppendLine("                   - resume      Resumes triggering of alerts");
                    report.AppendLine("                   - dump        Dumps all triggers as commands for copy and paste");
                    report.AppendLine("                   Defaults to 'list' if not specified.");
                    report.AppendLine();
                    report.AppendLine("  <collectionName> Required if the mode 'collection' is specified.");
                    report.AppendLine("                   Specifies the name of the collection to be triggered. Must be an exact match.");
                    report.AppendLine();
                    report.AppendLine("  <sourceName>     Optional. Specifies the name of a specific source. Must be an exact match.");
                    report.AppendLine();
                    report.AppendLine("  <threshold>      Required if the mode 'collection' is specified.");
                    report.AppendLine("                   Specifies the threshold in milliseconds (fractions allowed).");
                    report.AppendLine("                   Specify '0' to remove the threshold.");
                    report.AppendLine();
                    report.AppendLine("Examples:");
                    report.AppendLine();
                    report.AppendLine($"{CommandName} trigger collection Display.Rendering 10");
                    report.AppendLine("  Sets up an alert trigger which writes on the console if the execution time of all performance counters in");
                    report.AppendLine("  the 'Display.Rendering' collection exceed 10 milliseconds.");
                    report.AppendLine();
                    report.AppendLine($"{CommandName} trigger collection Display.Rendering 5 Pathoschild.ChestsAnywhere");
                    report.AppendLine("  Sets up an alert trigger to write on the console if the execution time of Pathoschild.ChestsAnywhere in");
                    report.AppendLine("  the 'Display.Rendering' collection exceed 5 milliseconds.");
                    report.AppendLine();
                    report.AppendLine($"{CommandName} trigger collection Display.Rendering 0");
                    report.AppendLine("  Removes the threshold previously defined from the collection. Note that source-specific thresholds are left intact.");
                    report.AppendLine();
                    report.AppendLine($"{CommandName} trigger clear");
                    report.AppendLine("  Clears all previously setup alert triggers.");
                    break;

                case SubCommand.Reset:
                    report.AppendLine($"Usage: {CommandName} reset <type> <name>");
                    report.AppendLine();
                    report.AppendLine("Resets performance counters.");
                    report.AppendLine();
                    report.AppendLine("Arguments:");
                    report.AppendLine("  <type>  Optional. Specifies if a collection or source should be reset.");
                    report.AppendLine("          If omitted, all performance counters are reset.");
                    report.AppendLine();
                    report.AppendLine("          - source     Clears performance counters for a specific source");
                    report.AppendLine("          - collection Clears performance counters for a specific collection");
                    report.AppendLine();
                    report.AppendLine("  <name>  Required if a <type> is given. Specifies the name of either the collection");
                    report.AppendLine("          or the source. The name must be an exact match.");
                    report.AppendLine();
                    report.AppendLine("Examples:");
                    report.AppendLine($"{CommandName} reset                                    Resets all performance counters");
                    report.AppendLine($"{CommandName} reset source Pathoschild.ChestsAnywhere  Resets all performance for the source named Pathoschild.ChestsAnywhere");
                    report.AppendLine($"{CommandName} reset collection Display.Rendering       Resets all performance for the collection named Display.Rendering");
                    break;
            }

            report.AppendLine();
            monitor.Log(report.ToString(), LogLevel.Info);
        }

        /// <summary>Get the command description.</summary>
        private static string GetDescription()
        {
            StringBuilder report = new StringBuilder();

            report.AppendLine("Displays or configures performance monitoring to diagnose issues. Performance monitoring is disabled by default.");
            report.AppendLine();
            report.AppendLine("For example, the counter collection named 'Display.Rendered' contains one performance");
            report.AppendLine("counter when the game executes the 'Display.Rendered' event, and another counter for each mod which handles it.");
            report.AppendLine();
            report.AppendLine($"Usage: {CommandName} <command> <action>");
            report.AppendLine();
            report.AppendLine("Commands:");
            report.AppendLine();
            report.AppendLine("  summary    Show a summary of collections.");
            report.AppendLine("  detail     Show a summary for a given collection.");
            report.AppendLine("  reset      Reset all performance counters.");
            report.AppendLine("  trigger    Configure alert triggers.");
            report.AppendLine("  enable     Enable performance counter recording.");
            report.AppendLine("  disable    Disable performance counter recording.");
            report.AppendLine("  help       Show verbose help for the available commands.");
            report.AppendLine();
            report.AppendLine($"To get help for a specific command, use '{CommandName} help <command>', for example:");
            report.AppendLine($"{CommandName} help summary");
            report.AppendLine();
            report.AppendLine("Defaults to summary if no command is given.");
            report.AppendLine();

            return report.ToString();
        }

        /// <summary>Log a warning if performance monitoring isn't enabled.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <returns>Returns whether performance monitoring is enabled.</returns>
        private bool AssertEnabled(IMonitor monitor)
        {
            if (!SCore.PerformanceMonitor.EnableTracking)
            {
                monitor.Log($"Performance monitoring is currently disabled; enter '{CommandName} enable' to enable it.", LogLevel.Warn);
                return false;
            }

            return true;
        }


        /*********
        ** Private models
        *********/
        /// <summary>An alert trigger for a collection.</summary>
        private class CollectionTrigger
        {
            /*********
            ** Accessors
            *********/
            /// <summary>The collection name.</summary>
            public string CollectionName { get; }

            /// <summary>The trigger threshold.</summary>
            public double Threshold { get; }


            /*********
            ** Public methods
            *********/
            /// <summary>Construct an instance.</summary>
            /// <param name="collectionName">The collection name.</param>
            /// <param name="threshold">The trigger threshold.</param>
            public CollectionTrigger(string collectionName, double threshold)
            {
                this.CollectionName = collectionName;
                this.Threshold = threshold;
            }
        }

        /// <summary>An alert triggered for a source.</summary>
        private class SourceTrigger : CollectionTrigger
        {
            /*********
            ** Accessors
            *********/
            /// <summary>The source name.</summary>
            public string SourceName { get; }


            /*********
            ** Public methods
            *********/
            /// <summary>Construct an instance.</summary>
            /// <param name="collectionName">The collection name.</param>
            /// <param name="sourceName">The source name.</param>
            /// <param name="threshold">The trigger threshold.</param>
            public SourceTrigger(string collectionName, string sourceName, double threshold)
                : base(collectionName, threshold)
            {
                this.SourceName = sourceName;
            }
        }
    }
}
