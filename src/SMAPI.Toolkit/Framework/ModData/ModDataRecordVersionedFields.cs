namespace StardewModdingAPI.Toolkit.Framework.ModData
{
    /// <summary>The versioned fields from a <see cref="ModDataRecord"/> for a specific manifest.</summary>
    public class ModDataRecordVersionedFields
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying data record.</summary>
        public ModDataRecord DataRecord { get; set; }

        /// <summary>The default mod name to display when the name isn't available (e.g. during dependency checks).</summary>
        public string DisplayName { get; set; }

        /// <summary>The update key to apply.</summary>
        public string UpdateKey { get; set; }

        /// <summary>The predefined compatibility status.</summary>
        public ModStatus Status { get; set; } = ModStatus.None;

        /// <summary>A reason phrase for the <see cref="Status"/>, or <c>null</c> to use the default reason.</summary>
        public string StatusReasonPhrase { get; set; }

        /// <summary>Technical details shown in TRACE logs for the <see cref="Status"/>, or <c>null</c> to omit it.</summary>
        public string StatusReasonDetails { get; set; }

        /// <summary>The upper version for which the <see cref="Status"/> applies (if any).</summary>
        public ISemanticVersion StatusUpperVersion { get; set; }
    }
}
