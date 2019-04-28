namespace StardewModdingAPI.Toolkit.Serialisation.Models
{
    /// <summary>Indicates which mod can read the content pack represented by the containing manifest.</summary>
    public class ManifestContentPackFor : IManifestContentPackFor
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique ID of the mod which can read this content pack.</summary>
        public string UniqueID { get; set; }

        /// <summary>The minimum required version (if any).</summary>
        public ISemanticVersion MinimumVersion { get; set; }
    }
}
