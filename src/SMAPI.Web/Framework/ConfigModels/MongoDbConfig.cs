namespace StardewModdingAPI.Web.Framework.ConfigModels
{
    /// <summary>The config settings for mod compatibility list.</summary>
    internal class MongoDbConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The MongoDB connection string.</summary>
        public string ConnectionString { get; set; }

        /// <summary>The database name.</summary>
        public string Database { get; set; }


        /*********
        ** Public method
        *********/
        /// <summary>Get whether a MongoDB instance is configured.</summary>
        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(this.ConnectionString);
        }
    }
}
