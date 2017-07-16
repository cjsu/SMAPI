using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace TrainerMod.Framework.Commands.Player
{
    /// <summary>A command which edits the player's current stamina.</summary>
    internal class SetStaminaCommand : TrainerCommand
    {
        /*********
        ** Properties
        *********/
        /// <summary>Whether to keep the player's stamina at its maximum.</summary>
        private bool InfiniteStamina;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the command needs to perform logic when the game updates.</summary>
        public override bool NeedsUpdate => this.InfiniteStamina;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetStaminaCommand()
            : base("player_setstamina", "Sets the player's stamina.\n\nUsage: player_setstamina [value]\n- value: an integer amount, or 'inf' for infinite stamina.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // validate
            if (!args.Any())
            {
                monitor.Log($"You currently have {(this.InfiniteStamina ? "infinite" : Game1.player.Stamina.ToString())} stamina. Specify a value to change it.", LogLevel.Info);
                return;
            }

            // handle
            string amountStr = args[0];
            if (amountStr == "inf")
            {
                this.InfiniteStamina = true;
                monitor.Log("OK, you now have infinite stamina.", LogLevel.Info);
            }
            else
            {
                this.InfiniteStamina = false;
                if (int.TryParse(amountStr, out int amount))
                {
                    Game1.player.Stamina = amount;
                    monitor.Log($"OK, you now have {Game1.player.Stamina} stamina.", LogLevel.Info);
                }
                else
                    this.LogArgumentNotInt(monitor);
            }
        }

        /// <summary>Perform any logic needed on update tick.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        public override void Update(IMonitor monitor)
        {
            if (this.InfiniteStamina)
                Game1.player.stamina = Game1.player.MaxStamina;
        }
    }
}
