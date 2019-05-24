namespace StardewModdingAPI.Framework.Networking
{
    /// <summary>Metadata about an installed mod exchanged with connected computers.</summary>
    public class RemoteContextModModel
    {
        /// <summary>The mod's display name.</summary>
        public string Name { get; set; }

        /// <summary>The unique mod ID.</summary>
        public string ID { get; set; }

        /// <summary>The mod version.</summary>
        public ISemanticVersion Version { get; set; }
    }
}
