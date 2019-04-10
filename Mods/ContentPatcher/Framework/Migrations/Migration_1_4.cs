using System.Diagnostics.CodeAnalysis;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common.Utilities;
using StardewModdingAPI;

namespace ContentPatcher.Framework.Migrations
{
    /// <summary>Migrate patches to format version 1.4.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named for clarity.")]
    internal class Migration_1_4 : BaseMigration
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public Migration_1_4()
            : base(new SemanticVersion(1, 4, 0))
        {
            this.AddedTokens = new InvariantHashSet
            {
                ConditionType.DayEvent.ToString(),
                ConditionType.HasFlag.ToString(),
                ConditionType.HasSeenEvent.ToString(),
                ConditionType.Hearts.ToString(),
                ConditionType.Relationship.ToString(),
                ConditionType.Spouse.ToString()
            };
        }
    }
}
