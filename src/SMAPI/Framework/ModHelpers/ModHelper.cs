using System;
using System.IO;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Toolkit.Serialization;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides simplified APIs for writing mods.</summary>
    internal class ModHelper : BaseHelper, IModHelper, IDisposable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The full path to the mod's folder.</summary>
        public string DirectoryPath { get; }

        /// <summary>Manages access to events raised by SMAPI, which let your mod react when something happens in the game.</summary>
        public IModEvents Events { get; }

        /// <summary>An API for loading content assets.</summary>
        public IContentHelper Content { get; }

        /// <summary>An API for managing content packs.</summary>
        public IContentPackHelper ContentPacks { get; }

        /// <summary>An API for reading and writing persistent mod data.</summary>
        public IDataHelper Data { get; }

        /// <summary>An API for checking and changing input state.</summary>
        public IInputHelper Input { get; }

        /// <summary>An API for accessing private game code.</summary>
        public IReflectionHelper Reflection { get; }

        /// <summary>an API for fetching metadata about loaded mods.</summary>
        public IModRegistry ModRegistry { get; }

        /// <summary>An API for managing console commands.</summary>
        public ICommandHelper ConsoleCommands { get; }

        /// <summary>Provides multiplayer utilities.</summary>
        public IMultiplayerHelper Multiplayer { get; }

        /// <summary>An API for reading translations stored in the mod's <c>i18n</c> folder, with one file per locale (like <c>en.json</c>) containing a flat key => value structure. Translations are fetched with locale fallback, so missing translations are filled in from broader locales (like <c>pt-BR.json</c> &lt; <c>pt.json</c> &lt; <c>default.json</c>).</summary>
        public ITranslationHelper Translation { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The mod's unique ID.</param>
        /// <param name="modDirectory">The full path to the mod's folder.</param>
        /// <param name="inputState">Manages the game's input state.</param>
        /// <param name="events">Manages access to events raised by SMAPI.</param>
        /// <param name="contentHelper">An API for loading content assets.</param>
        /// <param name="contentPackHelper">An API for managing content packs.</param>
        /// <param name="commandHelper">An API for managing console commands.</param>
        /// <param name="dataHelper">An API for reading and writing persistent mod data.</param>
        /// <param name="modRegistry">an API for fetching metadata about loaded mods.</param>
        /// <param name="reflectionHelper">An API for accessing private game code.</param>
        /// <param name="multiplayer">Provides multiplayer utilities.</param>
        /// <param name="translationHelper">An API for reading translations stored in the mod's <c>i18n</c> folder.</param>
        /// <exception cref="ArgumentNullException">An argument is null or empty.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="modDirectory"/> path does not exist on disk.</exception>
        public ModHelper(string modID, string modDirectory, SInputState inputState, IModEvents events, IContentHelper contentHelper, IContentPackHelper contentPackHelper, ICommandHelper commandHelper, IDataHelper dataHelper, IModRegistry modRegistry, IReflectionHelper reflectionHelper, IMultiplayerHelper multiplayer, ITranslationHelper translationHelper)
            : base(modID)
        {
            // validate directory
            if (string.IsNullOrWhiteSpace(modDirectory))
                throw new ArgumentNullException(nameof(modDirectory));
            if (!Directory.Exists(modDirectory))
                throw new InvalidOperationException("The specified mod directory does not exist.");

            // initialize
            this.DirectoryPath = modDirectory;
            this.Content = contentHelper ?? throw new ArgumentNullException(nameof(contentHelper));
            this.ContentPacks = contentPackHelper ?? throw new ArgumentNullException(nameof(contentPackHelper));
            this.Data = dataHelper ?? throw new ArgumentNullException(nameof(dataHelper));
            this.Input = new InputHelper(modID, inputState);
            this.ModRegistry = modRegistry ?? throw new ArgumentNullException(nameof(modRegistry));
            this.ConsoleCommands = commandHelper ?? throw new ArgumentNullException(nameof(commandHelper));
            this.Reflection = reflectionHelper ?? throw new ArgumentNullException(nameof(reflectionHelper));
            this.Multiplayer = multiplayer ?? throw new ArgumentNullException(nameof(multiplayer));
            this.Translation = translationHelper ?? throw new ArgumentNullException(nameof(translationHelper));
            this.Events = events;
        }

        /****
        ** Mod config file
        ****/
        /// <summary>Read the mod's configuration file (and create it if needed).</summary>
        /// <typeparam name="TConfig">The config class type. This should be a plain class that has public properties for the settings you want. These can be complex types.</typeparam>
        public TConfig ReadConfig<TConfig>()
            where TConfig : class, new()
        {
            TConfig config = this.Data.ReadJsonFile<TConfig>("config.json") ?? new TConfig();
            this.WriteConfig(config); // create file or fill in missing fields
            return config;
        }

        /// <summary>Save to the mod's configuration file.</summary>
        /// <typeparam name="TConfig">The config class type.</typeparam>
        /// <param name="config">The config settings to save.</param>
        public void WriteConfig<TConfig>(TConfig config)
            where TConfig : class, new()
        {
            this.Data.WriteJsonFile("config.json", config);
        }

        /****
        ** Disposal
        ****/
        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            // nothing to dispose yet
        }
    }
}
