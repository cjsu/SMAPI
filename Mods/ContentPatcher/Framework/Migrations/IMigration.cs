using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.ConfigModels;
using ContentPatcher.Framework.Tokens;
using StardewModdingAPI;

namespace ContentPatcher.Framework.Migrations
{
    /// <summary>Migrates patches to a given format version.</summary>
    internal interface IMigration
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The format version to which this migration applies.</summary>
        ISemanticVersion Version { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Migrate a content pack.</summary>
        /// <param name="content">The content pack data to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed.</param>
        /// <returns>Returns whether migration succeeded.</returns>
        bool TryMigrate(ContentConfig content, out string error);

        /// <summary>Migrate a token name.</summary>
        /// <param name="name">The token name to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed (if any).</param>
        /// <returns>Returns whether migration succeeded.</returns>
        bool TryMigrate(ref TokenName name, out string error);

        /// <summary>Migrate a tokenised string.</summary>
        /// <param name="tokenStr">The tokenised string to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed (if any).</param>
        /// <returns>Returns whether migration succeeded.</returns>
        bool TryMigrate(ref TokenString tokenStr, out string error);
    }
}
