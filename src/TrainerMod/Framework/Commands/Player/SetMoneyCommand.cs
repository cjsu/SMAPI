using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace TrainerMod.Framework.Commands.Player
{
    /// <summary>A command which edits the player's current money.</summary>
    internal class SetMoneyCommand : TrainerCommand
    {
        /*********
        ** Properties
        *********/
        /// <summary>Whether to keep the player's money at a set value.</summary>
        private bool InfiniteMoney;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the command needs to perform logic when the game updates.</summary>
        public override bool NeedsUpdate => this.InfiniteMoney;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetMoneyCommand()
            : base("player_setmoney", "Sets the player's money.\n\nUsage: player_setmoney <value>\n- value: an integer amount, or 'inf' for infinite money.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // validate
            if (!args.Any())
            {
                monitor.Log($"You currently have {(this.InfiniteMoney ? "infinite" : Game1.player.Money.ToString())} gold. Specify a value to change it.", LogLevel.Info);
                return;
            }

            // handle
            string amountStr = args[0];
            if (amountStr == "inf")
            {
                this.InfiniteMoney = true;
                monitor.Log("OK, you now have infinite money.", LogLevel.Info);
            }
            else
            {
                this.InfiniteMoney = false;
                if (int.TryParse(amountStr, out int amount))
                {
                    Game1.player.Money = amount;
                    monitor.Log($"OK, you now have {Game1.player.Money} gold.", LogLevel.Info);
                }
                else
                    this.LogArgumentNotInt(monitor);
            }
        }

        /// <summary>Perform any logic needed on update tick.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        public override void Update(IMonitor monitor)
        {
            if (this.InfiniteMoney)
                Game1.player.money = 999999;
        }
    }
}
