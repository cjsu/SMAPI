namespace StardewModdingAPI.Web.Framework.ConfigModels
{
    /// <summary>The site config settings.</summary>
    public class SiteConfig // must be public to pass into views
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The root URL for the app.</summary>
        public string RootUrl { get; set; }

        /// <summary>The root URL for the log parser.</summary>
        public string LogParserUrl { get; set; }

        /// <summary>The root URL for the JSON validator.</summary>
        public string JsonValidatorUrl { get; set; }

        /// <summary>The root URL for the mod list.</summary>
        public string ModListUrl { get; set; }

        /// <summary>Whether to show SMAPI beta versions on the main page, if any.</summary>
        public bool BetaEnabled { get; set; }

        /// <summary>A short sentence shown under the beta download button, if any.</summary>
        public string BetaBlurb { get; set; }
    }
}
