using System;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides an API for managing console commands.</summary>
    internal class CommandHelper : BaseHelper, ICommandHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod using this instance.</summary>
        private readonly IModMetadata Mod;

        /// <summary>Manages console commands.</summary>
        private readonly CommandManager CommandManager;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod using this instance.</param>
        /// <param name="commandManager">Manages console commands.</param>
        public CommandHelper(IModMetadata mod, CommandManager commandManager)
            : base(mod?.Manifest?.UniqueID ?? "SMAPI")
        {
            this.Mod = mod;
            this.CommandManager = commandManager;
        }

        /// <summary>Add a console command.</summary>
        /// <param name="name">The command name, which the user must type to trigger it.</param>
        /// <param name="documentation">The human-readable documentation shown when the player runs the built-in 'help' command.</param>
        /// <param name="callback">The method to invoke when the command is triggered. This method is passed the command name and arguments submitted by the user.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> or <paramref name="callback"/> is null or empty.</exception>
        /// <exception cref="FormatException">The <paramref name="name"/> is not a valid format.</exception>
        /// <exception cref="ArgumentException">There's already a command with that name.</exception>
        public ICommandHelper Add(string name, string documentation, Action<string, string[]> callback)
        {
            this.CommandManager.Add(this.Mod, name, documentation, callback);
            return this;
        }

        /// <summary>Trigger a command.</summary>
        /// <param name="name">The command name.</param>
        /// <param name="arguments">The command arguments.</param>
        /// <returns>Returns whether a matching command was triggered.</returns>
        public bool Trigger(string name, string[] arguments)
        {
            return this.CommandManager.Trigger(name, arguments);
        }
    }
}
