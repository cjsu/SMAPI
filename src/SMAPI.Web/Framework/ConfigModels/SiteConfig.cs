namespace StardewModdingAPI.Web.Framework.ConfigModels
{
    /// <summary>The site config settings.</summary>
    public class SiteConfig // must be public to pass into views
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether to show SMAPI beta versions on the main page, if any.</summary>
        public bool BetaEnabled { get; set; }

        /// <summary>A short sentence shown under the beta download button, if any.</summary>
        public string BetaBlurb { get; set; }

        /// <summary>A list of supports to credit on the main page, in Markdown format.</summary>
        public string SupporterList { get; set; }
    }
}
