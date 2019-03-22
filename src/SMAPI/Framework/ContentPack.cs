using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewModdingAPI.Toolkit.Utilities;
using xTile;

namespace StardewModdingAPI.Framework
{
    /// <summary>Manages access to a content pack's metadata and files.</summary>
    internal class ContentPack : IContentPack
    {
        /*********
        ** Fields
        *********/
        /// <summary>Provides an API for loading content assets.</summary>
        private readonly IContentHelper Content;

        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        private readonly JsonHelper JsonHelper;


        /*********
        ** Accessors
        *********/
        /// <summary>The full path to the content pack's folder.</summary>
        public string DirectoryPath { get; }

        /// <summary>The content pack's manifest.</summary>
        public IManifest Manifest { get; }

        /// <summary>Provides translations stored in the content pack's <c>i18n</c> folder. See <see cref="IModHelper.Translation"/> for more info.</summary>
        public ITranslationHelper Translation { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="directoryPath">The full path to the content pack's folder.</param>
        /// <param name="manifest">The content pack's manifest.</param>
        /// <param name="content">Provides an API for loading content assets.</param>
        /// <param name="translation">Provides translations stored in the content pack's <c>i18n</c> folder.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        public ContentPack(string directoryPath, IManifest manifest, IContentHelper content, ITranslationHelper translation, JsonHelper jsonHelper)
        {
            this.DirectoryPath = directoryPath;
            this.Manifest = manifest;
            this.Content = content;
            this.Translation = translation;
            this.JsonHelper = jsonHelper;
        }

        /// <summary>Get whether a given file exists in the content pack.</summary>
        /// <param name="path">The file path to check.</param>
        public bool HasFile(string path)
        {
            this.AssertRelativePath(path, nameof(this.HasFile));

            return File.Exists(Path.Combine(this.DirectoryPath, path));
        }

        /// <summary>Read a JSON file from the content pack folder.</summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <param name="path">The file path relative to the contnet directory.</param>
        /// <returns>Returns the deserialised model, or <c>null</c> if the file doesn't exist or is empty.</returns>
        /// <exception cref="InvalidOperationException">The <paramref name="path"/> is not relative or contains directory climbing (../).</exception>
        public TModel ReadJsonFile<TModel>(string path) where TModel : class
        {
            this.AssertRelativePath(path, nameof(this.ReadJsonFile));

            path = Path.Combine(this.DirectoryPath, PathUtilities.NormalisePathSeparators(path));
            return this.JsonHelper.ReadJsonFileIfExists(path, out TModel model)
                ? model
                : null;
        }

        /// <summary>Save data to a JSON file in the content pack's folder.</summary>
        /// <typeparam name="TModel">The model type. This should be a plain class that has public properties for the data you want. The properties can be complex types.</typeparam>
        /// <param name="path">The file path relative to the mod folder.</param>
        /// <param name="data">The arbitrary data to save.</param>
        /// <exception cref="InvalidOperationException">The <paramref name="path"/> is not relative or contains directory climbing (../).</exception>
        public void WriteJsonFile<TModel>(string path, TModel data) where TModel : class
        {
            this.AssertRelativePath(path, nameof(this.WriteJsonFile));

            path = Path.Combine(this.DirectoryPath, PathUtilities.NormalisePathSeparators(path));
            this.JsonHelper.WriteJsonFile(path, data);
        }

        /// <summary>Load content from the content pack folder (if not already cached), and return it. When loading a <c>.png</c> file, this must be called outside the game's draw loop.</summary>
        /// <typeparam name="T">The expected data type. The main supported types are <see cref="Map"/>, <see cref="Texture2D"/>, and dictionaries; other types may be supported by the game's content pipeline.</typeparam>
        /// <param name="key">The local path to a content file relative to the content pack folder.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        /// <exception cref="ContentLoadException">The content asset couldn't be loaded (e.g. because it doesn't exist).</exception>
        public T LoadAsset<T>(string key)
        {
            return this.Content.Load<T>(key, ContentSource.ModFolder);
        }

        /// <summary>Get the underlying key in the game's content cache for an asset. This can be used to load custom map tilesheets, but should be avoided when you can use the content API instead. This does not validate whether the asset exists.</summary>
        /// <param name="key">The the local path to a content file relative to the content pack folder.</param>
        /// <exception cref="ArgumentException">The <paramref name="key"/> is empty or contains invalid characters.</exception>
        public string GetActualAssetKey(string key)
        {
            return this.Content.GetActualAssetKey(key, ContentSource.ModFolder);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Assert that a relative path was passed it to a content pack method.</summary>
        /// <param name="path">The path to check.</param>
        /// <param name="methodName">The name of the method which was invoked.</param>
        private void AssertRelativePath(string path, string methodName)
        {
            if (!PathUtilities.IsSafeRelativePath(path))
                throw new InvalidOperationException($"You must call {nameof(IContentPack)}.{methodName} with a relative path.");
        }
    }
}
