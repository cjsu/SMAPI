namespace StardewModdingAPI.Framework.Networking
{
    internal class MultiplayerPeerMod : IMultiplayerPeerMod
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod's display name.</summary>
        public string Name { get; }

        /// <summary>The unique mod ID.</summary>
        public string ID { get; }

        /// <summary>The mod version.</summary>
        public ISemanticVersion Version { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod metadata.</param>
        public MultiplayerPeerMod(RemoteContextModModel mod)
        {
            this.Name = mod.Name;
            this.ID = mod.ID?.Trim();
            this.Version = mod.Version;
        }
    }
}
