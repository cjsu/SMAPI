namespace StardewModdingAPI
{
    /// <summary>A mod dependency listed in a mod manifest.</summary>
    public interface IManifestDependency
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique mod ID to require.</summary>
        string UniqueID { get; }
    }
}
