namespace StardewModdingAPI.Web.Framework.Clients.ModDrop.ResponseModels
{
    /// <summary>Metadata about a mod from the ModDrop API.</summary>
    public class ModDataModel
    {
        /// <summary>The mod's unique ID on ModDrop.</summary>
        public int ID { get; set; }

        /// <summary>The error code, if any.</summary>
        public int? ErrorCode { get; set; }

        /// <summary>The mod name.</summary>
        public string Title { get; set; }
    }
}
