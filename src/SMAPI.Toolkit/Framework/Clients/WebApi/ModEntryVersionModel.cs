namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi
{
    /// <summary>Metadata about a version.</summary>
    public class ModEntryVersionModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The version number.</summary>
        public ISemanticVersion Version { get; set; }

        /// <summary>The mod page URL.</summary>
        public string Url { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ModEntryVersionModel() { }

        /// <summary>Construct an instance.</summary>
        /// <param name="version">The version number.</param>
        /// <param name="url">The mod page URL.</param>
        public ModEntryVersionModel(ISemanticVersion version, string url)
        {
            this.Version = version;
            this.Url = url;
        }
    }
}
