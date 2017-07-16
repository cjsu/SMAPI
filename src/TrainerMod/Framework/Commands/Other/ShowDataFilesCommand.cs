using System.Diagnostics;
using StardewModdingAPI;

namespace TrainerMod.Framework.Commands.Other
{
    /// <summary>A command which shows the data files.</summary>
    internal class ShowDataFilesCommand : TrainerCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ShowDataFilesCommand()
            : base("show_data_files", "Opens the folder containing the save and log files.") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            Process.Start(Constants.DataPath);
            monitor.Log($"OK, opening {Constants.DataPath}.", LogLevel.Info);
        }
    }
}
