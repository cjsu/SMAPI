using System;
using System.Collections.Generic;
using System.IO;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Utilities;

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

        /// <summary>A cache of case-insensitive => exact relative paths within the content pack, for case-insensitive file lookups on Linux/Mac.</summary>
        private readonly IDictionary<string, string> RelativePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string DirectoryPath { get; }

        /// <inheritdoc />
        public IManifest Manifest { get; }

        /// <inheritdoc />
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

            foreach (string path in Directory.EnumerateFiles(this.DirectoryPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = path.Substring(this.DirectoryPath.Length + 1);
                this.RelativePaths[relativePath] = relativePath;
            }
        }

        /// <inheritdoc />
        public bool HasFile(string path)
        {
            path = PathUtilities.NormalizePath(path);

            return this.GetFile(path).Exists;
        }

        /// <inheritdoc />
        public TModel ReadJsonFile<TModel>(string path) where TModel : class
        {
            path = PathUtilities.NormalizePath(path);

            FileInfo file = this.GetFile(path);
            return file.Exists && this.JsonHelper.ReadJsonFileIfExists(file.FullName, out TModel model)
                ? model
                : null;
        }

        /// <inheritdoc />
        public void WriteJsonFile<TModel>(string path, TModel data) where TModel : class
        {
            path = PathUtilities.NormalizePath(path);

            FileInfo file = this.GetFile(path, out path);
            this.JsonHelper.WriteJsonFile(file.FullName, data);

            if (!this.RelativePaths.ContainsKey(path))
                this.RelativePaths[path] = path;
        }

        /// <inheritdoc />
        public T LoadAsset<T>(string key)
        {
            key = PathUtilities.NormalizePath(key);

            key = this.GetCaseInsensitiveRelativePath(key);
            return this.Content.Load<T>(key, ContentSource.ModFolder);
        }

        /// <inheritdoc />
        public string GetActualAssetKey(string key)
        {
            key = PathUtilities.NormalizePath(key);

            key = this.GetCaseInsensitiveRelativePath(key);
            return this.Content.GetActualAssetKey(key, ContentSource.ModFolder);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the real relative path from a case-insensitive path.</summary>
        /// <param name="relativePath">The normalized relative path.</param>
        private string GetCaseInsensitiveRelativePath(string relativePath)
        {
            if (!PathUtilities.IsSafeRelativePath(relativePath))
                throw new InvalidOperationException($"You must call {nameof(IContentPack)} methods with a relative path.");

            return !string.IsNullOrWhiteSpace(relativePath) && this.RelativePaths.TryGetValue(relativePath, out string caseInsensitivePath)
                ? caseInsensitivePath
                : relativePath;
        }

        /// <summary>Get the underlying file info.</summary>
        /// <param name="relativePath">The normalized file path relative to the content pack directory.</param>
        private FileInfo GetFile(string relativePath)
        {
            return this.GetFile(relativePath, out _);
        }

        /// <summary>Get the underlying file info.</summary>
        /// <param name="relativePath">The normalized file path relative to the content pack directory.</param>
        /// <param name="actualRelativePath">The relative path after case-insensitive matching.</param>
        private FileInfo GetFile(string relativePath, out string actualRelativePath)
        {
            actualRelativePath = this.GetCaseInsensitiveRelativePath(relativePath);
            return new FileInfo(Path.Combine(this.DirectoryPath, actualRelativePath));
        }
    }
}
