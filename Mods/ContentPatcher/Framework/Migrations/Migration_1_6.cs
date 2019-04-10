using System.Diagnostics.CodeAnalysis;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.ConfigModels;
using Pathoschild.Stardew.Common.Utilities;
using StardewModdingAPI;

namespace ContentPatcher.Framework.Migrations
{
    /// <summary>Migrate patches to format version 1.6.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named for clarity.")]
    internal class Migration_1_6 : BaseMigration
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public Migration_1_6()
            : base(new SemanticVersion(1, 6, 0))
        {
            this.AddedTokens = new InvariantHashSet
            {
                ConditionType.HasWalletItem.ToString(),
                ConditionType.SkillLevel.ToString()
            };
        }

        /// <summary>Migrate a content pack.</summary>
        /// <param name="content">The content pack data to migrate.</param>
        /// <param name="error">An error message which indicates why migration failed.</param>
        /// <returns>Returns whether the content pack was successfully migrated.</returns>
        public override bool TryMigrate(ContentConfig content, out string error)
        {
            if (!base.TryMigrate(content, out error))
                return false;

            // before 1.6, the 'sun' weather included 'wind'
            foreach (PatchConfig patch in content.Changes)
            {
                if (patch.When != null && patch.When.TryGetValue(ConditionType.Weather.ToString(), out string value) && value.Contains("Sun"))
                    patch.When[ConditionType.Weather.ToString()] = $"{value}, Wind";
            }

            return true;
        }
    }
}
