#if SMAPI_1_x
using StardewModdingAPI;
using StardewValley;

namespace TrainerMod.Framework.Commands.Saves
{
    /// <summary>A command which saves the game.</summary>
    internal class SaveCommand : TrainerCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SaveCommand()
            : base("save", "Saves the game? Doesn't seem to work.") { }


        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            monitor.Log("Saving the game...", LogLevel.Info);
            SaveGame.Save();
        }
    }
}
#endif