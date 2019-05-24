using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley.Network;

namespace StardewModdingAPI.Framework.Networking
{
    /// <summary>Metadata about a connected player.</summary>
    internal class MultiplayerPeer : IMultiplayerPeer
    {
        /*********
        ** Fields
        *********/
        /// <summary>A method which sends a message to the peer.</summary>
        private readonly Action<OutgoingMessage> SendMessageImpl;


        /*********
        ** Accessors
        *********/
        /// <summary>The player's unique ID.</summary>
        public long PlayerID { get; }

        /// <summary>Whether this is a connection to the host player.</summary>
        public bool IsHost { get; }

        /// <summary>Whether the player has SMAPI installed.</summary>
        public bool HasSmapi => this.ApiVersion != null;

        /// <summary>The player's OS platform, if <see cref="HasSmapi"/> is true.</summary>
        public GamePlatform? Platform { get; }

        /// <summary>The installed version of Stardew Valley, if <see cref="HasSmapi"/> is true.</summary>
        public ISemanticVersion GameVersion { get; }

        /// <summary>The installed version of SMAPI, if <see cref="HasSmapi"/> is true.</summary>
        public ISemanticVersion ApiVersion { get; }

        /// <summary>The installed mods, if <see cref="HasSmapi"/> is true.</summary>
        public IEnumerable<IMultiplayerPeerMod> Mods { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="playerID">The player's unique ID.</param>
        /// <param name="model">The metadata to copy.</param>
        /// <param name="sendMessage">A method which sends a message to the peer.</param>
        /// <param name="isHost">Whether this is a connection to the host player.</param>
        public MultiplayerPeer(long playerID, RemoteContextModel model, Action<OutgoingMessage> sendMessage, bool isHost)
        {
            this.PlayerID = playerID;
            this.IsHost = isHost;
            if (model != null)
            {
                this.Platform = model.Platform;
                this.GameVersion = model.GameVersion;
                this.ApiVersion = model.ApiVersion;
                this.Mods = model.Mods.Select(mod => new MultiplayerPeerMod(mod)).ToArray();
            }
            this.SendMessageImpl = sendMessage;
        }

        /// <summary>Get metadata for a mod installed by the player.</summary>
        /// <param name="id">The unique mod ID.</param>
        /// <returns>Returns the mod info, or <c>null</c> if the player doesn't have that mod.</returns>
        public IMultiplayerPeerMod GetMod(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || this.Mods == null || !this.Mods.Any())
                return null;

            id = id.Trim();
            return this.Mods.FirstOrDefault(mod => mod.ID != null && mod.ID.Equals(id, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>Send a message to the given peer, bypassing the game's normal validation to allow messages before the connection is approved.</summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(OutgoingMessage message)
        {
            this.SendMessageImpl(message);
        }
    }
}
