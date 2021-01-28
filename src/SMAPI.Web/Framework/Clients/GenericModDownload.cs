namespace StardewModdingAPI.Web.Framework.Clients
{
    /// <summary>Generic metadata about a file download on a mod page.</summary>
    internal class GenericModDownload : IModDownload
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The download's display name.</summary>
        public string Name { get; set; }

        /// <summary>The download's description.</summary>
        public string Description { get; set; }

        /// <summary>The download's file version.</summary>
        public string Version { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an empty instance.</summary>
        public GenericModDownload() { }

        /// <summary>Construct an instance.</summary>
        /// <param name="name">The download's display name.</param>
        /// <param name="description">The download's description.</param>
        /// <param name="version">The download's file version.</param>
        public GenericModDownload(string name, string description, string version)
        {
            this.Name = name;
            this.Description = description;
            this.Version = version;
        }
    }
}
