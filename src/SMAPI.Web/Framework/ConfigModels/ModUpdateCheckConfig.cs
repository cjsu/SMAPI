namespace StardewModdingAPI.Web.Framework.ConfigModels
{
    /// <summary>The config settings for mod update checks.</summary>
    internal class ModUpdateCheckConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The number of minutes successful update checks should be cached before refetching them.</summary>
        public int SuccessCacheMinutes { get; set; }

        /// <summary>The number of minutes failed update checks should be cached before refetching them.</summary>
        public int ErrorCacheMinutes { get; set; }
    }
}
