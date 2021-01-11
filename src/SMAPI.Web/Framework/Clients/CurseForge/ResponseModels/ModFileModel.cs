namespace StardewModdingAPI.Web.Framework.Clients.CurseForge.ResponseModels
{
    /// <summary>Metadata from the CurseForge API about a mod file.</summary>
    public class ModFileModel
    {
        /// <summary>The file name as downloaded.</summary>
        public string FileName { get; set; }

        /// <summary>The file display name.</summary>
        public string DisplayName { get; set; }
    }
}
