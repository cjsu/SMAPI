using ContentPatcher.Framework.ConfigModels;
using ContentPatcher.Framework.Migrations;
using StardewModdingAPI;

namespace ContentPatcher.Framework
{
    /// <summary>A content pack being loaded.</summary>
    internal class RawContentPack
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The managed content pack instance.</summary>
        public ManagedContentPack ManagedPack { get; }

        /// <summary>The raw content configuration for this content pack.</summary>
        public ContentConfig Content { get; }

        /// <summary>The migrations to apply for the content pack version.</summary>
        public IMigration Migrator { get; }
        
        /// <summary>The content pack's manifest.</summary>
        public IManifest Manifest => this.ManagedPack.Manifest;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="contentPack">The managed content pack instance.</param>
        /// <param name="content">The raw content configuration for this content pack.</param>
        /// <param name="migrator">The migrations to apply for the content pack version.</param>
        public RawContentPack(ManagedContentPack contentPack, ContentConfig content, IMigration migrator)
        {
            this.ManagedPack = contentPack;
            this.Content = content;
            this.Migrator = migrator;
        }
    }
}
