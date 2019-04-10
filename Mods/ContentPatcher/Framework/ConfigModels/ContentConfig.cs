using Pathoschild.Stardew.Common.Utilities;
using StardewModdingAPI;

namespace ContentPatcher.Framework.ConfigModels
{
    /// <summary>The model for a content patch file.</summary>
    internal class ContentConfig
    {
        /// <summary>The format version.</summary>
        public ISemanticVersion Format { get; set; }

        /// <summary>The user-defined tokens whose values may depend on other tokens.</summary>
        public DynamicTokenConfig[] DynamicTokens { get; set; }

        /// <summary>The changes to make.</summary>
        public PatchConfig[] Changes { get; set; }

        /// <summary>The schema for the <c>config.json</c> file (if any).</summary>
        public InvariantDictionary<ConfigSchemaFieldConfig> ConfigSchema { get; set; }
    }
}
