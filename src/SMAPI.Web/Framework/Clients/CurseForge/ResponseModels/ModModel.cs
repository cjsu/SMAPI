namespace StardewModdingAPI.Web.Framework.Clients.CurseForge.ResponseModels
{
    /// <summary>An mod from the CurseForge API.</summary>
    public class ModModel
    {
        /// <summary>The mod's unique ID on CurseForge.</summary>
        public int ID { get; set; }

        /// <summary>The mod name.</summary>
        public string Name { get; set; }

        /// <summary>The web URL for the mod page.</summary>
        public string WebsiteUrl { get; set; }

        /// <summary>The available file downloads.</summary>
        public ModFileModel[] LatestFiles { get; set; }
    }
}
