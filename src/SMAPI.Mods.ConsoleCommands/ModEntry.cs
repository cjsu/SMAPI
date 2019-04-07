using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Mods.ConsoleCommands.Framework.Commands;

namespace StardewModdingAPI.Mods.ConsoleCommands
{
    /// <summary>The main entry point for the mod.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>The commands to handle.</summary>
        private ITrainerCommand[] Commands;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // register commands
            this.Commands = this.ScanForCommands().ToArray();
            foreach (ITrainerCommand command in this.Commands)
                helper.ConsoleCommands.Add(command.Name, command.Description, (name, args) => this.HandleCommand(command, name, args));

            // hook events
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method invoked when the game updates its state.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            foreach (ITrainerCommand command in this.Commands)
            {
                if (command.NeedsUpdate)
                    command.Update(this.Monitor);
            }
        }

        /// <summary>Handle a console command.</summary>
        /// <param name="command">The command to invoke.</param>
        /// <param name="commandName">The command name specified by the user.</param>
        /// <param name="args">The command arguments.</param>
        private void HandleCommand(ITrainerCommand command, string commandName, string[] args)
        {
            ArgumentParser argParser = new ArgumentParser(commandName, args, this.Monitor);
            command.Handle(this.Monitor, commandName, argParser);
        }

        /// <summary>Find all commands in the assembly.</summary>
        private IEnumerable<ITrainerCommand> ScanForCommands()
        {
            return (
                from type in this.GetType().Assembly.GetTypes()
                where !type.IsAbstract && typeof(ITrainerCommand).IsAssignableFrom(type)
                select (ITrainerCommand)Activator.CreateInstance(type)
            );
        }
    }
}
