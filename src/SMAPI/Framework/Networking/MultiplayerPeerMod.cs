namespace StardewModdingAPI.Framework.Networking
{
    internal class MultiplayerPeerMod : IMultiplayerPeerMod
    {
        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public string ID { get; }

        /// <inheritdoc />
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
