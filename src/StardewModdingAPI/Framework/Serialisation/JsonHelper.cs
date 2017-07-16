using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using StardewModdingAPI.Utilities;

namespace StardewModdingAPI.Framework.Serialisation
{
    /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
    internal class JsonHelper
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The JSON settings to use when serialising and deserialising files.</summary>
        private readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ObjectCreationHandling = ObjectCreationHandling.Replace, // avoid issue where default ICollection<T> values are duplicated each time the config is loaded
            Converters = new List<JsonConverter>
            {
                new SelectiveStringEnumConverter(typeof(Buttons), typeof(Keys), typeof(SButton))
            }
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Read a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="fullPath">The absolete file path.</param>
        /// <returns>Returns the deserialised model, or <c>null</c> if the file doesn't exist or is empty.</returns>
        /// <exception cref="InvalidOperationException">The given path is empty or invalid.</exception>
        public TModel ReadJsonFile<TModel>(string fullPath)
            where TModel : class
        {
            // validate
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("The file path is empty or invalid.", nameof(fullPath));

            // read file
            string json;
            try
            {
                json = File.ReadAllText(fullPath);
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is FileNotFoundException)
            {
                return null;
            }

            // deserialise model
            try
            {
                return JsonConvert.DeserializeObject<TModel>(json, this.JsonSettings);
            }
            catch (JsonReaderException ex)
            {
                string message = $"The file at {fullPath} doesn't seem to be valid JSON.";

                string text = File.ReadAllText(fullPath);
                if (text.Contains("“") || text.Contains("”"))
                    message += " Found curly quotes in the text; note that only straight quotes are allowed in JSON.";

                message += $"\nTechnical details: {ex.Message}";
                throw new JsonReaderException(message);
            }
        }

        /// <summary>Save to a JSON file.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="fullPath">The absolete file path.</param>
        /// <param name="model">The model to save.</param>
        /// <exception cref="InvalidOperationException">The given path is empty or invalid.</exception>
        public void WriteJsonFile<TModel>(string fullPath, TModel model)
            where TModel : class
        {
            // validate
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException("The file path is empty or invalid.", nameof(fullPath));

            // create directory if needed
            string dir = Path.GetDirectoryName(fullPath);
            if (dir == null)
                throw new ArgumentException("The file path is invalid.", nameof(fullPath));
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // write file
            string json = JsonConvert.SerializeObject(model, this.JsonSettings);
            File.WriteAllText(fullPath, json);
        }
    }
}
