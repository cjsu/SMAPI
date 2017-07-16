using StardewModdingAPI;
using StardewValley;

namespace TrainerMod.Framework.Commands.Player
{
    /// <summary>A command which edits the player's current added speed.</summary>
    internal class SetSpeedCommand : TrainerCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetSpeedCommand()
            : base("player_setspeed", "Sets the player's added speed to the specified value.\n\nUsage: player_setspeed <value>\n- value: an integer amount (0 is normal).") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // parse arguments
            if (!args.TryGetInt(0, "added speed", out int amount, min: 0))
                return;

            // handle
            Game1.player.addedSpeed = amount;
            monitor.Log($"OK, your added speed is now {Game1.player.addedSpeed}.", LogLevel.Info);
        }
    }
}
