using Newtonsoft.Json;

namespace StardewModdingAPI.Web.Framework.Clients.CurseForge
{
    /// <summary>Mod metadata from the CurseForge API.</summary>
    internal class CurseForgeMod
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod name.</summary>
        public string Name { get; set; }

        /// <summary>The latest file version.</summary>
        public string LatestVersion { get; set; }

        /// <summary>The mod's web URL.</summary>
        public string Url { get; set; }

        /// <summary>A user-friendly error which indicates why fetching the mod info failed (if applicable).</summary>
        public string Error { get; set; }
    }
}
