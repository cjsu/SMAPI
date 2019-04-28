namespace StardewModdingAPI.Toolkit.Serialisation.Models
{
    /// <summary>A mod dependency listed in a mod manifest.</summary>
    public class ManifestDependency : IManifestDependency
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique mod ID to require.</summary>
        public string UniqueID { get; set; }

        /// <summary>The minimum required version (if any).</summary>
        public ISemanticVersion MinimumVersion { get; set; }

        /// <summary>Whether the dependency must be installed to use the mod.</summary>
        public bool IsRequired { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="uniqueID">The unique mod ID to require.</param>
        /// <param name="minimumVersion">The minimum required version (if any).</param>
        /// <param name="required">Whether the dependency must be installed to use the mod.</param>
        public ManifestDependency(string uniqueID, string minimumVersion, bool required = true)
        {
            this.UniqueID = uniqueID;
            this.MinimumVersion = !string.IsNullOrWhiteSpace(minimumVersion)
                ? new SemanticVersion(minimumVersion)
                : null;
            this.IsRequired = required;
        }
    }
}
