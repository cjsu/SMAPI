using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>Events raised when something changes in the world.</summary>
    internal class ModWorldEvents : ModEventsBase, IWorldEvents
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Raised after a game location is added or removed.</summary>
        public event EventHandler<LocationListChangedEventArgs> LocationListChanged
        {
            add => this.EventManager.LocationListChanged.Add(value, this.Mod);
            remove => this.EventManager.LocationListChanged.Remove(value);
        }

        /// <summary>Raised after buildings are added or removed in a location.</summary>
        public event EventHandler<BuildingListChangedEventArgs> BuildingListChanged
        {
            add => this.EventManager.BuildingListChanged.Add(value, this.Mod);
            remove => this.EventManager.BuildingListChanged.Remove(value);
        }

        /// <summary>Raised after debris are added or removed in a location.</summary>
        public event EventHandler<DebrisListChangedEventArgs> DebrisListChanged
        {
            add => this.EventManager.DebrisListChanged.Add(value, this.Mod);
            remove => this.EventManager.DebrisListChanged.Remove(value);
        }

        /// <summary>Raised after large terrain features (like bushes) are added or removed in a location.</summary>
        public event EventHandler<LargeTerrainFeatureListChangedEventArgs> LargeTerrainFeatureListChanged
        {
            add => this.EventManager.LargeTerrainFeatureListChanged.Add(value, this.Mod);
            remove => this.EventManager.LargeTerrainFeatureListChanged.Remove(value);
        }

        /// <summary>Raised after NPCs are added or removed in a location.</summary>
        public event EventHandler<NpcListChangedEventArgs> NpcListChanged
        {
            add => this.EventManager.NpcListChanged.Add(value);
            remove => this.EventManager.NpcListChanged.Remove(value);
        }

        /// <summary>Raised after objects are added or removed in a location.</summary>
        public event EventHandler<ObjectListChangedEventArgs> ObjectListChanged
        {
            add => this.EventManager.ObjectListChanged.Add(value);
            remove => this.EventManager.ObjectListChanged.Remove(value);
        }

        /// <summary>Raised after terrain features (like floors and trees) are added or removed in a location.</summary>
        public event EventHandler<TerrainFeatureListChangedEventArgs> TerrainFeatureListChanged
        {
            add => this.EventManager.TerrainFeatureListChanged.Add(value);
            remove => this.EventManager.TerrainFeatureListChanged.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModWorldEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
