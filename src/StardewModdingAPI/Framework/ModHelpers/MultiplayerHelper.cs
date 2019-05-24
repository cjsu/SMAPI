using System;
using System.Collections.Generic;
using StardewModdingAPI.Framework.Networking;
using StardewValley;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides multiplayer utilities.</summary>
    internal class MultiplayerHelper : BaseHelper, IMultiplayerHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>SMAPI's core multiplayer utility.</summary>
        private readonly SMultiplayer Multiplayer;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="multiplayer">SMAPI's core multiplayer utility.</param>
        public MultiplayerHelper(string modID, SMultiplayer multiplayer)
            : base(modID)
        {
            this.Multiplayer = multiplayer;
        }

        /// <summary>Get a new multiplayer ID.</summary>
        public long GetNewID()
        {
            return this.Multiplayer.getNewID();
        }

        /// <summary>Get the locations which are being actively synced from the host.</summary>
        public IEnumerable<GameLocation> GetActiveLocations()
        {
            return this.Multiplayer.activeLocations();
        }

        /// <summary>Get a connected player.</summary>
        /// <param name="id">The player's unique ID.</param>
        /// <returns>Returns the connected player, or <c>null</c> if no such player is connected.</returns>
        public IMultiplayerPeer GetConnectedPlayer(long id)
        {
            return this.Multiplayer.Peers.TryGetValue(id, out MultiplayerPeer peer)
                ? peer
                : null;
        }

        /// <summary>Get all connected players.</summary>
        public IEnumerable<IMultiplayerPeer> GetConnectedPlayers()
        {
            return this.Multiplayer.Peers.Values;
        }

        /// <summary>Send a message to mods installed by connected players.</summary>
        /// <typeparam name="TMessage">The data type. This can be a class with a default constructor, or a value type.</typeparam>
        /// <param name="message">The data to send over the network.</param>
        /// <param name="messageType">A message type which receiving mods can use to decide whether it's the one they want to handle, like <c>SetPlayerLocation</c>. This doesn't need to be globally unique, since mods should check the originating mod ID.</param>
        /// <param name="modIDs">The mod IDs which should receive the message on the destination computers, or <c>null</c> for all mods. Specifying mod IDs is recommended to improve performance, unless it's a general-purpose broadcast.</param>
        /// <param name="playerIDs">The <see cref="Farmer.UniqueMultiplayerID" /> values for the players who should receive the message, or <c>null</c> for all players. If you don't need to broadcast to all players, specifying player IDs is recommended to reduce latency.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="message"/> or <paramref name="messageType" /> is null.</exception>
        public void SendMessage<TMessage>(TMessage message, string messageType, string[] modIDs = null, long[] playerIDs = null)
        {
            this.Multiplayer.BroadcastModMessage(
                message: message,
                messageType: messageType,
                fromModID: this.ModID,
                toModIDs: modIDs,
                toPlayerIDs: playerIDs
            );
        }
    }
}
