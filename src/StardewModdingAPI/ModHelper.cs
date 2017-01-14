using System;
using System.IO;
using Newtonsoft.Json;
using StardewModdingAPI.Advanced;
using StardewModdingAPI.Framework.Reflection;

namespace StardewModdingAPI
{
    /// <summary>Provides simplified APIs for writing mods.</summary>
    [Obsolete("Use " + nameof(IModHelper) + " instead.")] // only direct mod access to this class is obsolete
    public class ModHelper : IModHelper
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod directory path.</summary>
        public string DirectoryPath { get; }

        /// <summary>Simplifies access to private game code.</summary>
        public IReflectionHelper Reflection { get; } = new ReflectionHelper();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modDirectory">The mod directory path.</param>
        public ModHelper(string modDirectory)
        {
            // validate
            if (string.IsNullOrWhiteSpace(modDirectory))
                throw new InvalidOperationException("The mod directory cannot be empty.");
            if (!Directory.Exists(modDirectory))
                throw new InvalidOperationException("The specified mod directory does not exist.");

            // initialise
            this.DirectoryPath = modDirectory;
        }

        /****
        ** Mod config file
        ****/
        /// <summary>Read the mod's configuration file (and create it if needed).</summary>
        /// <typeparam name="TConfig">The config class type. This should be a plain class that has public properties for the settings you want. These can be complex types.</typeparam>
        public TConfig ReadConfig<TConfig>()
            where TConfig : class, new()
        {
            var config = this.ReadJsonFile<TConfig>("config.json") ?? new TConfig();
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
            // read file
            string fullPath = Path.Combine(this.DirectoryPath, path);
            string json;
            try
            {
                json = File.ReadAllText(fullPath);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }

            // deserialise model
            TModel model = JsonConvert.DeserializeObject<TModel>(json);
            if (model is IConfigFile)
            {
                var wrapper = (IConfigFile)model;
                wrapper.ModHelper = this;
                wrapper.FilePath = path;
            }

            return model;
        }

        /// <summary>Save to a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="path">The file path relative to the mod directory.</param>
        /// <param name="model">The model to save.</param>
        public void WriteJsonFile<TModel>(string path, TModel model)
            where TModel : class
        {
            path = Path.Combine(this.DirectoryPath, path);

            // create directory if needed
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // write file
            string json = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
