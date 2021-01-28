using System;
using System.Collections.Generic;
using System.IO;
using StardewModdingAPI.Toolkit.Serialization.Models;

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

        /// <inheritdoc />
        public IEnumerable<IContentPack> GetOwned()
        {
            return this.ContentPacks.Value;
        }

        /// <inheritdoc />
        public IContentPack CreateFake(string directoryPath)
        {
            string id = Guid.NewGuid().ToString("N");
            return this.CreateTemporary(directoryPath, id, id, id, id, new SemanticVersion(1, 0, 0));
        }

        /// <inheritdoc />
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
