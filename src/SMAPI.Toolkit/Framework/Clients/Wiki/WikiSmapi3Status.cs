namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki
{
    /// <summary>Whether a mod is ready for the upcoming SMAPI 3.0.</summary>
    public enum WikiSmapi3Status
    {
        /// <summary>The mod's compatibility status is unknown.</summary>
        Unknown = 0,

        /// <summary>The mod is compatible with the upcoming SMAPI 3.0.</summary>
        Ok = 1,

        /// <summary>The mod will break in SMAPI 3.0.</summary>
        Broken = 2,

        /// <summary>The mod has a pull request submitted for SMAPI 3.0 compatibility.</summary>
        Soon = 3
    }
}
