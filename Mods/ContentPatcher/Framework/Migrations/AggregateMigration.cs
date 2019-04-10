using System.Collections.Generic;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.ConfigModels;
using ContentPatcher.Framework.Tokens;
using StardewModdingAPI;

namespace ContentPatcher.Framework.Migrations
{
    /// <summary>Aggregates content pack migrations.</summary>
    internal class AggregateMigration : IMigration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The valid format versions.</summary>
        private readonly HashSet<string> ValidVersions;

        /// <summary>The migrations to apply.</summary>
        private readonly IMigration[] Migrations;


        /*********
        ** Accessors
        *********/
        /// <summary>The version to which this migration applies.</summary>
        public ISemanticVersion Version { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="version">The content pack version.</param>
        /// <param name="validVersions">The valid format versions.</param>
        /// <param name="migrations">The migrations to apply.</param>
        public AggregateMigration(ISemanticVersion version, string[] validVersions, IMigration[] migrations)
        {
            this.Version = version;
            this.ValidVersions = new HashSet<string>(validVersions);
            this.Migrations = migrations.Where(m => m.Version.IsNewerThan(version)).ToArray();
        }

        /// <summary>Migrate a content pack.</summary>
        /// <param name="content">The content pack data to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed.</param>
        /// <returns>Returns whether the content pack was successfully migrated.</returns>
        public bool TryMigrate(ContentConfig content, out string error)
        {
            // validate format version
            if (!this.ValidVersions.Contains(content.Format.ToString()))
            {
                error = $"unsupported format {content.Format} (supported version: {string.Join(", ", this.ValidVersions)}).";
                return false;
            }

            // apply migrations
            foreach (IMigration migration in this.Migrations)
            {
                if (!migration.TryMigrate(content, out error))
                    return false;
            }

            // no issues found
            error = null;
            return true;
        }

        /// <summary>Migrate a token name.</summary>
        /// <param name="name">The token name to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed (if any).</param>
        /// <returns>Returns whether migration succeeded.</returns>
        public bool TryMigrate(ref TokenName name, out string error)
        {
            // apply migrations
            foreach (IMigration migration in this.Migrations)
            {
                if (!migration.TryMigrate(ref name, out error))
                    return false;
            }

            // no issues found
            error = null;
            return true;
        }

        /// <summary>Migrate a tokenised string.</summary>
        /// <param name="tokenStr">The tokenised string to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed (if any).</param>
        /// <returns>Returns whether migration succeeded.</returns>
        public bool TryMigrate(ref TokenString tokenStr, out string error)
        {
            // apply migrations
            foreach (IMigration migration in this.Migrations)
            {
                if (!migration.TryMigrate(ref tokenStr, out error))
                    return false;
            }

            // no issues found
            error = null;
            return true;
        }
    }
}
