using System;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.World
{
    /// <summary>A command which sets the current time.</summary>
    internal class SetTimeCommand : ConsoleCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetTimeCommand()
            : base("world_settime", "Sets the time to the specified value.\n\nUsage: world_settime <value>\n- value: the target time in military time (like 0600 for 6am and 1800 for 6pm).") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // no-argument mode
            if (!args.Any())
            {
                monitor.Log($"The current time is {Game1.timeOfDay}. Specify a value to change it.", LogLevel.Info);
                return;
            }

            // parse arguments
            if (!args.TryGetInt(0, "time", out int time, min: 600, max: 2600))
                return;

            // handle
            this.SafelySetTime(time);
            FreezeTimeCommand.FrozenTime = Game1.timeOfDay;
            monitor.Log($"OK, the time is now {Game1.timeOfDay.ToString().PadLeft(4, '0')}.", LogLevel.Info);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Safely transition to the given time, allowing NPCs to update their schedule.</summary>
        /// <param name="time">The time of day.</param>
        private void SafelySetTime(int time)
        {
            // define conversion between game time and TimeSpan
            TimeSpan ToTimeSpan(int value) => new TimeSpan(0, value / 100, value % 100, 0);
            int FromTimeSpan(TimeSpan span) => (span.Hours * 100) + span.Minutes;

            // transition to new time
            int intervals = (int)((ToTimeSpan(time) - ToTimeSpan(Game1.timeOfDay)).TotalMinutes / 10);
            if (intervals > 0)
            {
                for (int i = 0; i < intervals; i++)
                    Game1.performTenMinuteClockUpdate();
            }
            else if (intervals < 0)
            {
                for (int i = 0; i > intervals; i--)
                {
                    Game1.timeOfDay = FromTimeSpan(ToTimeSpan(Game1.timeOfDay).Subtract(TimeSpan.FromMinutes(20))); // offset 20 minutes so game updates to next interval
                    Game1.performTenMinuteClockUpdate();
                }
            }
        }
    }
}
