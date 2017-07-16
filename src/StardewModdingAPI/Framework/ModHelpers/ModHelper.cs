using System;
using System.IO;
using StardewModdingAPI.Framework.Serialisation;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides simplified APIs for writing mods.</summary>
    internal class ModHelper : BaseHelper, IModHelper, IDisposable
    {
        /*********
        ** Properties
        *********/
        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;


        /*********
        ** Accessors
        *********/
        /// <summary>The full path to the mod's folder.</summary>
        public string DirectoryPath { get; }

        /// <summary>An API for loading content assets.</summary>
        public IContentHelper Content { get; }

        /// <summary>An API for accessing private game code.</summary>
        public IReflectionHelper Reflection { get; }

        /// <summary>an API for fetching metadata about loaded mods.</summary>
        public IModRegistry ModRegistry { get; }

        /// <summary>An API for managing console commands.</summary>
        public ICommandHelper ConsoleCommands { get; }

        /// <summary>An API for reading translations stored in the mod's <c>i18n</c> folder, with one file per locale (like <c>en.json</c>) containing a flat key => value structure. Translations are fetched with locale fallback, so missing translations are filled in from broader locales (like <c>pt-BR.json</c> &lt; <c>pt.json</c> &lt; <c>default.json</c>).</summary>
        public ITranslationHelper Translation { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The mod's unique ID.</param>
        /// <param name="modDirectory">The full path to the mod's folder.</param>
        /// <param name="jsonHelper">Encapsulate SMAPI's JSON parsing.</param>
        /// <param name="contentHelper">An API for loading content assets.</param>
        /// <param name="commandHelper">An API for managing console commands.</param>
        /// <param name="modRegistry">an API for fetching metadata about loaded mods.</param>
        /// <param name="reflectionHelper">An API for accessing private game code.</param>
        /// <param name="translationHelper">An API for reading translations stored in the mod's <c>i18n</c> folder.</param>
        /// <exception cref="ArgumentNullException">An argument is null or empty.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="modDirectory"/> path does not exist on disk.</exception>
        public ModHelper(string modID, string modDirectory, JsonHelper jsonHelper, IContentHelper contentHelper, ICommandHelper commandHelper, IModRegistry modRegistry, IReflectionHelper reflectionHelper, ITranslationHelper translationHelper)
            : base(modID)
        {
            // validate directory
            if (string.IsNullOrWhiteSpace(modDirectory))
                throw new ArgumentNullException(nameof(modDirectory));
            if (!Directory.Exists(modDirectory))
                throw new InvalidOperationException("The specified mod directory does not exist.");

            // initialise
            this.DirectoryPath = modDirectory;
            this.JsonHelper = jsonHelper ?? throw new ArgumentNullException(nameof(jsonHelper));
            this.Content = contentHelper ?? throw new ArgumentNullException(nameof(contentHelper));
            this.ModRegistry = modRegistry ?? throw new ArgumentNullException(nameof(modRegistry));
            this.ConsoleCommands = commandHelper ?? throw new ArgumentNullException(nameof(commandHelper));
            this.Reflection = reflectionHelper ?? throw new ArgumentNullException(nameof(reflectionHelper));
            this.Translation = translationHelper ?? throw new ArgumentNullException(nameof(translationHelper));
        }

        /****
        ** Mod config file
        ****/
        /// <summary>Read the mod's configuration file (and create it if needed).</summary>
        /// <typeparam name="TConfig">The config class type. This should be a plain class that has public properties for the settings you want. These can be complex types.</typeparam>
        public TConfig ReadConfig<TConfig>()
            where TConfig : class, new()
        {
            TConfig config = this.ReadJsonFile<TConfig>("config.json") ?? new TConfig();
            this.WriteConfig(config); // create file or fill in missing fields
            return config;
        }

        /// <summary>Save to the mod's configuration file.</summary>
        /// <typeparam name="TConfig">The config class type.</typeparam>
        /// <param name="config">The config settings to save.</param>
        public void WriteConfig<TConfig>(TConfig config)
            where TConfig : class, new()
        {
            this.WriteJsonFile("config.json", config);
        }

        /****
        ** Generic JSON files
        ****/
        /// <summary>Read a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="path">The file path relative to the mod directory.</param>
        /// <returns>Returns the deserialised model, or <c>null</c> if the file doesn't exist or is empty.</returns>
        public TModel ReadJsonFile<TModel>(string path)
            where TModel : class
        {
            path = Path.Combine(this.DirectoryPath, path);
            return this.JsonHelper.ReadJsonFile<TModel>(path);
        }

        /// <summary>Save to a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="path">The file path relative to the mod directory.</param>
        /// <param name="model">The model to save.</param>
        public void WriteJsonFile<TModel>(string path, TModel model)
            where TModel : class
        {
            path = Path.Combine(this.DirectoryPath, path);
            this.JsonHelper.WriteJsonFile(path, model);
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
