namespace ContentPatcher.Framework.ConfigModels
{
    /// <summary>The schema for a field in the <c>config.json</c> file.</summary>
    internal class ConfigSchemaFieldConfig
    {
        /// <summary>The comma-delimited values to allow.</summary>
        public string AllowValues { get; set; }

        /// <summary>The default value if the field is missing or (if <see cref="AllowBlank"/> is <c>false</c>) blank.</summary>
        public string Default { get; set; }

        /// <summary>Whether to allow blank values.</summary>
        public bool AllowBlank { get; set; } = false;

        /// <summary>Whether the player can specify multiple values for this field.</summary>
        public bool AllowMultiple { get; set; } = false;
    }
}
