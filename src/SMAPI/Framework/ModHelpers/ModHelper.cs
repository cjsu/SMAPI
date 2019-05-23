using System;
using System.Collections.Generic;
using System.IO;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewModdingAPI.Toolkit.Serialisation.Models;
using StardewModdingAPI.Toolkit.Utilities;

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

#if !SMAPI_3_0_STRICT
        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;
#endif

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
        /// <param name="jsonHelper">Encapsulate SMAPI's JSON parsing.</param>
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
        public ModHelper(string modID, string modDirectory, JsonHelper jsonHelper, SInputState inputState, IModEvents events, IContentHelper contentHelper, IContentPackHelper contentPackHelper, ICommandHelper commandHelper, IDataHelper dataHelper, IModRegistry modRegistry, IReflectionHelper reflectionHelper, IMultiplayerHelper multiplayer, ITranslationHelper translationHelper)
            : base(modID)
        {
            // validate directory
            if (string.IsNullOrWhiteSpace(modDirectory))
                throw new ArgumentNullException(nameof(modDirectory));
            if (!Directory.Exists(modDirectory))
                throw new InvalidOperationException("The specified mod directory does not exist.");

            // initialise
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
#if !SMAPI_3_0_STRICT
            this.JsonHelper = jsonHelper ?? throw new ArgumentNullException(nameof(jsonHelper));
#endif
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

#if !SMAPI_3_0_STRICT
        /****
        ** Generic JSON files
        ****/
        /// <summary>Read a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="path">The file path relative to the mod directory.</param>
        /// <returns>Returns the deserialised model, or <c>null</c> if the file doesn't exist or is empty.</returns>
        [Obsolete("Use " + nameof(ModHelper.Data) + "." + nameof(IDataHelper.ReadJsonFile) + " instead")]
        public TModel ReadJsonFile<TModel>(string path)
            where TModel : class
        {
            path = Path.Combine(this.DirectoryPath, PathUtilities.NormalisePathSeparators(path));
            return this.JsonHelper.ReadJsonFileIfExists(path, out TModel data)
                ? data
                : null;
        }

        /// <summary>Save to a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="path">The file path relative to the mod directory.</param>
        /// <param name="model">The model to save.</param>
        [Obsolete("Use " + nameof(ModHelper.Data) + "." + nameof(IDataHelper.WriteJsonFile) + " instead")]
        public void WriteJsonFile<TModel>(string path, TModel model)
            where TModel : class
        {
            path = Path.Combine(this.DirectoryPath, PathUtilities.NormalisePathSeparators(path));
            this.JsonHelper.WriteJsonFile(path, model);
        }
#endif

        /****
        ** Content packs
        ****/
#if !SMAPI_3_0_STRICT
        /// <summary>Manually create a transitional content pack to support pre-SMAPI content packs. This provides a way to access legacy content packs using the SMAPI content pack APIs, but the content pack will not be visible in the log or validated by SMAPI.</summary>
        /// <param name="directoryPath">The absolute directory path containing the content pack files.</param>
        /// <param name="id">The content pack's unique ID.</param>
        /// <param name="name">The content pack name.</param>
        /// <param name="description">The content pack description.</param>
        /// <param name="author">The content pack author's name.</param>
        /// <param name="version">The content pack version.</param>
        [Obsolete("Use " + nameof(IModHelper) + "." + nameof(IModHelper.ContentPacks) + "." + nameof(IContentPackHelper.CreateTemporary) + " instead")]
        public IContentPack CreateTransitionalContentPack(string directoryPath, string id, string name, string description, string author, ISemanticVersion version)
        {
            SCore.DeprecationManager.Warn($"{nameof(IModHelper)}.{nameof(IModHelper.CreateTransitionalContentPack)}", "2.5", DeprecationLevel.PendingRemoval);
            return this.ContentPacks.CreateTemporary(directoryPath, id, name, description, author, version);
        }

        /// <summary>Get all content packs loaded for this mod.</summary>
        [Obsolete("Use " + nameof(IModHelper) + "." + nameof(IModHelper.ContentPacks) + "." + nameof(IContentPackHelper.GetOwned) + " instead")]
        public IEnumerable<IContentPack> GetContentPacks()
        {
            return this.ContentPacks.GetOwned();
        }
#endif

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
