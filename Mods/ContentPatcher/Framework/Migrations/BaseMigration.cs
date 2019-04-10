using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.ConfigModels;
using ContentPatcher.Framework.Tokens;
using Pathoschild.Stardew.Common.Utilities;
using StardewModdingAPI;

namespace ContentPatcher.Framework.Migrations
{
    /// <summary>The base implementation for a format version migrator.</summary>
    internal abstract class BaseMigration : IMigration
    {
        /*********
        ** Private methods
        *********/
        /// <summary>The tokens added in this format version.</summary>
        protected InvariantHashSet AddedTokens { get; set; }


        /*********
        ** Accessors
        *********/
        /// <summary>The format version to which this migration applies.</summary>
        public ISemanticVersion Version { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Migrate a content pack.</summary>
        /// <param name="content">The content pack data to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed.</param>
        /// <returns>Returns whether the content pack was successfully migrated.</returns>
        public virtual bool TryMigrate(ContentConfig content, out string error)
        {
            error = null;
            return true;
        }

        /// <summary>Migrate a token name.</summary>
        /// <param name="name">The token name to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed (if any).</param>
        /// <returns>Returns whether migration succeeded.</returns>
        public virtual bool TryMigrate(ref TokenName name, out string error)
        {
            // tokens which need a higher version
            if (this.AddedTokens.Contains(name.Key))
            {
                error = this.GetNounPhraseError($"using token {name}");
                return false;
            }

            // no issue found
            error = null;
            return true;
        }

        /// <summary>Migrate a tokenised string.</summary>
        /// <param name="tokenStr">The tokenised string to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed (if any).</param>
        /// <returns>Returns whether migration succeeded.</returns>
        public virtual bool TryMigrate(ref TokenString tokenStr, out string error)
        {
            // tokens which need a high version
            foreach (TokenName token in tokenStr.Tokens)
            {
                if (this.AddedTokens.Contains(token.Key))
                {
                    error = this.GetNounPhraseError($"using token {token.Key}");
                    return false;
                }
            }

            // no issue found
            error = null;
            return true;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="version">The version to which this migration applies.</param>
        protected BaseMigration(ISemanticVersion version)
        {
            this.Version = version;
        }

        /// <summary>Get an error message indicating an action or feature requires a newer format version.</summary>
        /// <param name="nounPhrase">The noun phrase, like "using X feature".</param>
        protected string GetNounPhraseError(string nounPhrase)
        {
            return $"{nounPhrase} requires {nameof(ContentConfig.Format)} version {this.Version} or later";
        }
    }
}
