namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki
{
    /// <summary>Metadata from the wiki's mod compatibility list.</summary>
    public class WikiModList
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The stable game version.</summary>
        public string StableVersion { get; set; }

        /// <summary>The beta game version (if any).</summary>
        public string BetaVersion { get; set; }

        /// <summary>The mods on the wiki.</summary>
        public WikiModEntry[] Mods { get; set; }
    }
}
