#if SMAPI_1_x
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace TrainerMod.Framework.Commands.Saves
{
    /// <summary>A command which shows the load screen.</summary>
    internal class LoadCommand : TrainerCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public LoadCommand()
            : base("load", "Shows the load screen.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            monitor.Log("Triggering load menu...", LogLevel.Info);
            Game1.hasLoadedGame = false;
            Game1.activeClickableMenu = new LoadGameMenu();
        }
    }
}
#endif