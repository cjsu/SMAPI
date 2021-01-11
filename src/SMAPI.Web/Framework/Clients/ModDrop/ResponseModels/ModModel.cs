namespace StardewModdingAPI.Web.Framework.Clients.ModDrop.ResponseModels
{
    /// <summary>An entry in a mod list from the ModDrop API.</summary>
    public class ModModel
    {
        /// <summary>The available file downloads.</summary>
        public FileDataModel[] Files { get; set; }

        /// <summary>The mod metadata.</summary>
        public ModDataModel Mod { get; set; }
    }
}
