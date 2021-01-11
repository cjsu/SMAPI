using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands.Player
{
    /// <summary>A command which edits the player's current health.</summary>
    internal class SetHealthCommand : TrainerCommand
    {
        /*********
        ** Fields
        *********/
        /// <summary>Whether to keep the player's health at its maximum.</summary>
        private bool InfiniteHealth;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetHealthCommand()
            : base("player_sethealth", "Sets the player's health.\n\nUsage: player_sethealth [value]\n- value: an integer amount, or 'inf' for infinite health.", mayNeedUpdate: true) { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // no-argument mode
            if (!args.Any())
            {
                monitor.Log($"You currently have {(this.InfiniteHealth ? "infinite" : Game1.player.health.ToString())} health. Specify a value to change it.", LogLevel.Info);
                return;
            }

            // handle
            string amountStr = args[0];
            if (amountStr == "inf")
            {
                this.InfiniteHealth = true;
                monitor.Log("OK, you now have infinite health.", LogLevel.Info);
            }
            else
            {
                this.InfiniteHealth = false;
                if (int.TryParse(amountStr, out int amount))
                {
                    Game1.player.health = amount;
                    monitor.Log($"OK, you now have {Game1.player.health} health.", LogLevel.Info);
                }
                else
                    this.LogArgumentNotInt(monitor);
            }
        }

        /// <summary>Perform any logic needed on update tick.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        public override void OnUpdated(IMonitor monitor)
        {
            if (this.InfiniteHealth && Context.IsWorldReady)
                Game1.player.health = Game1.player.maxHealth;
        }
    }
}
