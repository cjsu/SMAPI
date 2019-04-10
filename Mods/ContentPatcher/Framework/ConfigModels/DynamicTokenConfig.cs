using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.ConfigModels
{
    /// <summary>A user-defined token whose value may depend on other tokens.</summary>
    internal class DynamicTokenConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The name of the token to set.</summary>
        public string Name { get; set; }

        /// <summary>The value to set.</summary>
        public string Value { get; set; }

        /// <summary>The criteria to apply. See readme for valid values.</summary>
        public InvariantDictionary<string> When { get; set; }
    }
}
