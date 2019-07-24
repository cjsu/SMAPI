namespace StardewModdingAPI.Web.Framework.Clients.ModDrop
{
    /// <summary>Mod metadata from the ModDrop API.</summary>
    internal class ModDropMod
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod name.</summary>
        public string Name { get; set; }

        /// <summary>The latest default file version.</summary>
        public ISemanticVersion LatestDefaultVersion { get; set; }

        /// <summary>The latest optional file version.</summary>
        public ISemanticVersion LatestOptionalVersion { get; set; }

        /// <summary>The mod's web URL.</summary>
        public string Url { get; set; }
    }
}
