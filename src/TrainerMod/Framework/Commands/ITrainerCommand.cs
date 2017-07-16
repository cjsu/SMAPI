using StardewModdingAPI;

namespace TrainerMod.Framework.Commands
{
    /// <summary>A TrainerMod command to register.</summary>
    internal interface ITrainerCommand
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The command name the user must type.</summary>
        string Name { get; }

        /// <summary>The command description.</summary>
        string Description { get; }

        /// <summary>Whether the command needs to perform logic when the game updates.</summary>
        bool NeedsUpdate { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        void Handle(IMonitor monitor, string command, ArgumentParser args);

        /// <summary>Perform any logic needed on update tick.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        void Update(IMonitor monitor);
    }
}
