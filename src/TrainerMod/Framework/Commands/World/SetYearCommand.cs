using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace TrainerMod.Framework.Commands.World
{
    /// <summary>A command which sets the current year.</summary>
    internal class SetYearCommand : TrainerCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetYearCommand()
            : base("world_setyear", "Sets the year to the specified value.\n\nUsage: world_setyear <year>\n- year: the target year (a number starting from 1).") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // no-argument mode
            if (!args.Any())
            {
                monitor.Log($"The current year is {Game1.year}. Specify a value to change the year.", LogLevel.Info);
                return;
            }

            // parse arguments
            if (!args.TryGetInt(0, "year", out int year, min: 1))
                return;

            // handle
            Game1.year = year;
            monitor.Log($"OK, the year is now {Game1.year}.", LogLevel.Info);
        }
    }
}
