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


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an empty instance.</summary>
        public ModSearchEntryModel()
        {
            // needed for JSON deserialising
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="id">The unique mod ID.</param>
        /// <param name="updateKeys">The namespaced mod update keys (if available).</param>
        public ModSearchEntryModel(string id, string[] updateKeys)
        {
            this.ID = id;
            this.UpdateKeys = updateKeys ?? new string[0];
        }
    }
}
