using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.StateTracking.FieldWatchers;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace StardewModdingAPI.Framework.StateTracking
{
    /// <summary>Tracks changes to a location's data.</summary>
    internal class LocationTracker : IWatcher
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying watchers.</summary>
        private readonly List<IWatcher> Watchers = new List<IWatcher>();


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the value changed since the last reset.</summary>
        public bool IsChanged => this.Watchers.Any(p => p.IsChanged);

        /// <summary>The tracked location.</summary>
        public GameLocation Location { get; }

        /// <summary>Tracks added or removed buildings.</summary>
        public ICollectionWatcher<Building> BuildingsWatcher { get; }

        /// <summary>Tracks added or removed debris.</summary>
        public ICollectionWatcher<Debris> DebrisWatcher { get; }

        /// <summary>Tracks added or removed large terrain features.</summary>
        public ICollectionWatcher<LargeTerrainFeature> LargeTerrainFeaturesWatcher { get; }

        /// <summary>Tracks added or removed NPCs.</summary>
        public ICollectionWatcher<NPC> NpcsWatcher { get; }

        /// <summary>Tracks added or removed objects.</summary>
        public IDictionaryWatcher<Vector2, Object> ObjectsWatcher { get; }

        /// <summary>Tracks added or removed terrain features.</summary>
        public IDictionaryWatcher<Vector2, TerrainFeature> TerrainFeaturesWatcher { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="location">The location to track.</param>
        public LocationTracker(GameLocation location)
        {
            this.Location = location;

            // init watchers
            this.BuildingsWatcher = location is BuildableGameLocation buildableLocation ? WatcherFactory.ForNetCollection(buildableLocation.buildings) : WatcherFactory.ForImmutableCollection<Building>();
            this.DebrisWatcher = WatcherFactory.ForNetCollection(location.debris);
            this.LargeTerrainFeaturesWatcher = WatcherFactory.ForNetCollection(location.largeTerrainFeatures);
            this.NpcsWatcher = WatcherFactory.ForNetCollection(location.characters);
            this.ObjectsWatcher = WatcherFactory.ForNetDictionary(location.netObjects);
            this.TerrainFeaturesWatcher = WatcherFactory.ForNetDictionary(location.terrainFeatures);

            this.Watchers.AddRange(new IWatcher[]
            {
                this.BuildingsWatcher,
                this.DebrisWatcher,
                this.LargeTerrainFeaturesWatcher,
                this.NpcsWatcher,
                this.ObjectsWatcher,
                this.TerrainFeaturesWatcher
            });
        }

        /// <summary>Stop watching the player fields and release all references.</summary>
        public void Dispose()
        {
            foreach (IWatcher watcher in this.Watchers)
                watcher.Dispose();
        }

        /// <summary>Update the current value if needed.</summary>
        public void Update()
        {
            foreach (IWatcher watcher in this.Watchers)
                watcher.Update();
        }

        /// <summary>Set the current value as the baseline.</summary>
        public void Reset()
        {
            foreach (IWatcher watcher in this.Watchers)
                watcher.Reset();
        }
    }
}
