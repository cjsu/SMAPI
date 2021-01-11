namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi
{
    /// <summary>Specifies the identifiers for a mod to match.</summary>
    public class ModSearchEntryModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique mod ID.</summary>
        public string ID { get; set; }

        /// <summary>The namespaced mod update keys (if available).</summary>
        public string[] UpdateKeys { get; set; }

        /// <summary>The mod version installed by the local player. This is used for version mapping in some cases.</summary>
        public ISemanticVersion InstalledVersion { get; set; }

        /// <summary>Whether the installed version is broken or could not be loaded.</summary>
        public bool IsBroken { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an empty instance.</summary>
        public ModSearchEntryModel()
        {
            // needed for JSON deserializing
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="id">The unique mod ID.</param>
        /// <param name="installedVersion">The version installed by the local player. This is used for version mapping in some cases.</param>
        /// <param name="updateKeys">The namespaced mod update keys (if available).</param>
        /// <param name="isBroken">Whether the installed version is broken or could not be loaded.</param>
        public ModSearchEntryModel(string id, ISemanticVersion installedVersion, string[] updateKeys, bool isBroken = false)
        {
            this.ID = id;
            this.InstalledVersion = installedVersion;
            this.UpdateKeys = updateKeys ?? new string[0];
        }
    }
}
