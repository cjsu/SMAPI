namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki
{
    /// <summary>The compatibility status for a mod.</summary>
    public enum WikiCompatibilityStatus
    {
        /// <summary>The mod is compatible.</summary>
        Ok = 0,

        /// <summary>The mod is compatible if you use an optional official download.</summary>
        Optional = 1,

        /// <summary>The mod is compatible if you use an unofficial update.</summary>
        Unofficial = 2,

        /// <summary>The mod isn't compatible, but the player can fix it or there's a good alternative.</summary>
        Workaround = 3,

        /// <summary>The mod isn't compatible.</summary>
        Broken = 4,

        /// <summary>The mod is no longer maintained by the author, and an unofficial update or continuation is unlikely.</summary>
        Abandoned = 5,

        /// <summary>The mod is no longer needed and should be removed.</summary>
        Obsolete = 6
    }
}
