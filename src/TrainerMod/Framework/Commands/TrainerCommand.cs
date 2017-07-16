using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;

namespace TrainerMod.Framework.Commands
{
    /// <summary>The base implementation for a trainer command.</summary>
    internal abstract class TrainerCommand : ITrainerCommand
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The command name the user must type.</summary>
        public string Name { get; }

        /// <summary>The command description.</summary>
        public string Description { get; }

        /// <summary>Whether the command needs to perform logic when the game updates.</summary>
        public virtual bool NeedsUpdate { get; } = false;


        /*********
        ** Public methods
        *********/
        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public abstract void Handle(IMonitor monitor, string command, ArgumentParser args);

        /// <summary>Perform any logic needed on update tick.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        public virtual void Update(IMonitor monitor) { }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">The command name the user must type.</param>
        /// <param name="description">The command description.</param>
        protected TrainerCommand(string name, string description)
        {
            this.Name = name;
            this.Description = description;
        }

        /// <summary>Log an error indicating incorrect usage.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="error">A sentence explaining the problem.</param>
        protected void LogUsageError(IMonitor monitor, string error)
        {
            monitor.Log($"{error} Type 'help {this.Name}' for usage.", LogLevel.Error);
        }

        /// <summary>Log an error indicating a value must be an integer.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        protected void LogArgumentNotInt(IMonitor monitor)
        {
            this.LogUsageError(monitor, "The value must be a whole number.");
        }

        /// <summary>Get an ASCII table to show tabular data in the console.</summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <param name="data">The data to display.</param>
        /// <param name="header">The table header.</param>
        /// <param name="getRow">Returns a set of fields for a data value.</param>
        protected string GetTableString<T>(IEnumerable<T> data, string[] header, Func<T, string[]> getRow)
        {
            // get table data
            int[] widths = header.Select(p => p.Length).ToArray();
            string[][] rows = data
                .Select(item =>
                {
                    string[] fields = getRow(item);
                    if (fields.Length != widths.Length)
                        throw new InvalidOperationException($"Expected {widths.Length} columns, but found {fields.Length}: {string.Join(", ", fields)}");

                    for (int i = 0; i < fields.Length; i++)
                        widths[i] = Math.Max(widths[i], fields[i].Length);

                    return fields;
                })
                .ToArray();

            // render fields
            List<string[]> lines = new List<string[]>(rows.Length + 2)
            {
                header,
                header.Select((value, i) => "".PadRight(widths[i], '-')).ToArray()
            };
            lines.AddRange(rows);

            return string.Join(
                Environment.NewLine,
                lines.Select(line => string.Join(" | ", line.Select((field, i) => field.PadRight(widths[i], ' ')).ToArray())
                )
            );
        }
    }
}
