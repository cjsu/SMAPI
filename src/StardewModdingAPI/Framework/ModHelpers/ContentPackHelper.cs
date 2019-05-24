using System;
using System.Collections.Generic;
using System.IO;
using StardewModdingAPI.Toolkit.Serialisation.Models;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides an API for managing content packs.</summary>
    internal class ContentPackHelper : BaseHelper, IContentPackHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>The content packs loaded for this mod.</summary>
        private readonly Lazy<IContentPack[]> ContentPacks;

        /// <summary>Create a temporary content pack.</summary>
        private readonly Func<string, IManifest, IContentPack> CreateContentPack;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="contentPacks">The content packs loaded for this mod.</param>
        /// <param name="createContentPack">Create a temporary content pack.</param>
        public ContentPackHelper(string modID, Lazy<IContentPack[]> contentPacks, Func<string, IManifest, IContentPack> createContentPack)
            : base(modID)
        {
            this.ContentPacks = contentPacks;
            this.CreateContentPack = createContentPack;
        }

        /// <summary>Get all content packs loaded for this mod.</summary>
        public IEnumerable<IContentPack> GetOwned()
        {
            return this.ContentPacks.Value;
        }

        /// <summary>Create a temporary content pack to read files from a directory, using randomised manifest fields. This will generate fake manifest data; any <c>manifest.json</c> in the directory will be ignored. Temporary content packs will not appear in the SMAPI log and update checks will not be performed.</summary>
        /// <param name="directoryPath">The absolute directory path containing the content pack files.</param>
        public IContentPack CreateFake(string directoryPath)
        {
            string id = Guid.NewGuid().ToString("N");
            return this.CreateTemporary(directoryPath, id, id, id, id, new SemanticVersion(1, 0, 0));
        }

        /// <summary>Create a temporary content pack to read files from a directory. Temporary content packs will not appear in the SMAPI log and update checks will not be performed.</summary>
        /// <param name="directoryPath">The absolute directory path containing the content pack files.</param>
        /// <param name="id">The content pack's unique ID.</param>
        /// <param name="name">The content pack name.</param>
        /// <param name="description">The content pack description.</param>
        /// <param name="author">The content pack author's name.</param>
        /// <param name="version">The content pack version.</param>
        public IContentPack CreateTemporary(string directoryPath, string id, string name, string description, string author, ISemanticVersion version)
        {
            // validate
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentNullException(nameof(directoryPath));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (!Directory.Exists(directoryPath))
                throw new ArgumentException($"Can't create content pack for directory path '{directoryPath}' because no such directory exists.");

            // create manifest
            IManifest manifest = new Manifest(
                uniqueID: id,
                name: name,
                author: author,
                description: description,
                version: version,
                contentPackFor: this.ModID
            );

            // create content pack
            return this.CreateContentPack(directoryPath, manifest);
        }
    }
}
