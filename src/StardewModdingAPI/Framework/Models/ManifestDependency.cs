namespace StardewModdingAPI.Framework.Models
{
    /// <summary>A mod dependency listed in a mod manifest.</summary>
    internal class ManifestDependency : IManifestDependency
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique mod ID to require.</summary>
        public string UniqueID { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="uniqueID">The unique mod ID to require.</param>
        public ManifestDependency(string uniqueID)
        {
            this.UniqueID = uniqueID;
        }
    }
}
