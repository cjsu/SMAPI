using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>Events raised for multiplayer messages and connections.</summary>
    internal class ModMultiplayerEvents : ModEventsBase, IMultiplayerEvents
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Raised after the mod context for a peer is received. This happens before the game approves the connection (<see cref="IMultiplayerEvents.PeerConnected"/>), so the player doesn't yet exist in the game. This is the earliest point where messages can be sent to the peer via SMAPI.</summary>
        public event EventHandler<PeerContextReceivedEventArgs> PeerContextReceived
        {
            add => this.EventManager.PeerContextReceived.Add(value, this.Mod);
            remove => this.EventManager.PeerContextReceived.Remove(value);
        }

        /// <summary>Raised after a peer connection is approved by the game.</summary>
        public event EventHandler<PeerConnectedEventArgs> PeerConnected
        {
            add => this.EventManager.PeerConnected.Add(value, this.Mod);
            remove => this.EventManager.PeerConnected.Remove(value);
        }

        /// <summary>Raised after a mod message is received over the network.</summary>
        public event EventHandler<ModMessageReceivedEventArgs> ModMessageReceived
        {
            add => this.EventManager.ModMessageReceived.Add(value, this.Mod);
            remove => this.EventManager.ModMessageReceived.Remove(value);
        }

        /// <summary>Raised after the connection with a peer is severed.</summary>
        public event EventHandler<PeerDisconnectedEventArgs> PeerDisconnected
        {
            add => this.EventManager.PeerDisconnected.Add(value, this.Mod);
            remove => this.EventManager.PeerDisconnected.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModMultiplayerEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
