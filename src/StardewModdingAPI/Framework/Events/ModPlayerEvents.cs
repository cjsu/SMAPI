using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>Events raised when the player data changes.</summary>
    internal class ModPlayerEvents : ModEventsBase, IPlayerEvents
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Raised after items are added or removed to a player's inventory. NOTE: this event is currently only raised for the local player.</summary>
        public event EventHandler<InventoryChangedEventArgs> InventoryChanged
        {
            add => this.EventManager.InventoryChanged.Add(value);
            remove => this.EventManager.InventoryChanged.Remove(value);
        }

        /// <summary>Raised after a player skill level changes. This happens as soon as they level up, not when the game notifies the player after their character goes to bed.  NOTE: this event is currently only raised for the local player.</summary>
        public event EventHandler<LevelChangedEventArgs> LevelChanged
        {
            add => this.EventManager.LevelChanged.Add(value);
            remove => this.EventManager.LevelChanged.Remove(value);
        }

        /// <summary>Raised after a player warps to a new location. NOTE: this event is currently only raised for the local player.</summary>
        public event EventHandler<WarpedEventArgs> Warped
        {
            add => this.EventManager.Warped.Add(value);
            remove => this.EventManager.Warped.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModPlayerEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
