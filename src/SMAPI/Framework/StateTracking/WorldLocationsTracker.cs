using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StardewModdingAPI.Framework.StateTracking.Comparers;
using StardewModdingAPI.Framework.StateTracking.FieldWatchers;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace StardewModdingAPI.Framework.StateTracking
{
    /// <summary>Detects changes to the game's locations.</summary>
    internal class WorldLocationsTracker : IWatcher
    {
        /*********
        ** Fields
        *********/
        /// <summary>Tracks changes to the location list.</summary>
        private readonly ICollectionWatcher<GameLocation> LocationListWatcher;

        /// <summary>Tracks changes to the list of active mine locations.</summary>
        private readonly ICollectionWatcher<MineShaft> MineLocationListWatcher;

        /// <summary>A lookup of the tracked locations.</summary>
        private IDictionary<GameLocation, LocationTracker> LocationDict { get; } = new Dictionary<GameLocation, LocationTracker>(new ObjectReferenceComparer<GameLocation>());

        /// <summary>A lookup of registered buildings and their indoor location.</summary>
        private readonly IDictionary<Building, GameLocation> BuildingIndoors = new Dictionary<Building, GameLocation>(new ObjectReferenceComparer<Building>());


        /*********
        ** Accessors
        *********/
        /// <summary>Whether locations were added or removed since the last reset.</summary>
        public bool IsLocationListChanged => this.Added.Any() || this.Removed.Any();

        /// <summary>Whether any tracked location data changed since the last reset.</summary>
        public bool IsChanged => this.IsLocationListChanged || this.Locations.Any(p => p.IsChanged);

        /// <summary>The tracked locations.</summary>
        public IEnumerable<LocationTracker> Locations => this.LocationDict.Values;

        /// <summary>The locations removed since the last update.</summary>
        public ICollection<GameLocation> Added { get; } = new HashSet<GameLocation>(new ObjectReferenceComparer<GameLocation>());

        /// <summary>The locations added since the last update.</summary>
        public ICollection<GameLocation> Removed { get; } = new HashSet<GameLocation>(new ObjectReferenceComparer<GameLocation>());


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="locations">The game's list of locations.</param>
        /// <param name="activeMineLocations">The game's list of active mine locations.</param>
        public WorldLocationsTracker(ObservableCollection<GameLocation> locations, IList<MineShaft> activeMineLocations)
        {
            this.LocationListWatcher = WatcherFactory.ForObservableCollection(locations);
            this.MineLocationListWatcher = WatcherFactory.ForReferenceList(activeMineLocations);
        }

        /// <summary>Update the current value if needed.</summary>
        public void Update()
        {
            // update watchers
            this.LocationListWatcher.Update();
            this.MineLocationListWatcher.Update();
            foreach (LocationTracker watcher in this.Locations)
                watcher.Update();

            // detect added/removed locations
            if (this.LocationListWatcher.IsChanged)
            {
                this.Remove(this.LocationListWatcher.Removed);
                this.Add(this.LocationListWatcher.Added);
            }
            if (this.MineLocationListWatcher.IsChanged)
            {
                this.Remove(this.MineLocationListWatcher.Removed);
                this.Add(this.MineLocationListWatcher.Added);
            }

            // detect building changed
            foreach (LocationTracker watcher in this.Locations.Where(p => p.BuildingsWatcher.IsChanged).ToArray())
            {
                this.Remove(watcher.BuildingsWatcher.Removed);
                this.Add(watcher.BuildingsWatcher.Added);
            }

            // detect building interiors changed (e.g. construction completed)
            foreach (KeyValuePair<Building, GameLocation> pair in this.BuildingIndoors.Where(p => !object.Equals(p.Key.indoors.Value, p.Value)))
            {
                GameLocation oldIndoors = pair.Value;
                GameLocation newIndoors = pair.Key.indoors.Value;

                if (oldIndoors != null)
                    this.Added.Add(oldIndoors);
                if (newIndoors != null)
                    this.Removed.Add(newIndoors);
            }
        }

        /// <summary>Set the current location list as the baseline.</summary>
        public void ResetLocationList()
        {
            this.Removed.Clear();
            this.Added.Clear();
            this.LocationListWatcher.Reset();
            this.MineLocationListWatcher.Reset();
        }

        /// <summary>Set the current value as the baseline.</summary>
        public void Reset()
        {
            this.ResetLocationList();
            foreach (IWatcher watcher in this.GetWatchers())
                watcher.Reset();
        }

        /// <summary>Stop watching the player fields and release all references.</summary>
        public void Dispose()
        {
            foreach (IWatcher watcher in this.GetWatchers())
                watcher.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Enumerable wrappers
        ****/
        /// <summary>Add the given buildings.</summary>
        /// <param name="buildings">The buildings to add.</param>
        public void Add(IEnumerable<Building> buildings)
        {
            foreach (Building building in buildings)
                this.Add(building);
        }

        /// <summary>Add the given locations.</summary>
        /// <param name="locations">The locations to add.</param>
        public void Add(IEnumerable<GameLocation> locations)
        {
            foreach (GameLocation location in locations)
                this.Add(location);
        }

        /// <summary>Remove the given buildings.</summary>
        /// <param name="buildings">The buildings to remove.</param>
        public void Remove(IEnumerable<Building> buildings)
        {
            foreach (Building building in buildings)
                this.Remove(building);
        }

        /// <summary>Remove the given locations.</summary>
        /// <param name="locations">The locations to remove.</param>
        public void Remove(IEnumerable<GameLocation> locations)
        {
            foreach (GameLocation location in locations)
                this.Remove(location);
        }

        /****
        ** Main add/remove logic
        ****/
        /// <summary>Add the given building.</summary>
        /// <param name="building">The building to add.</param>
        public void Add(Building building)
        {
            if (building == null)
                return;

            GameLocation indoors = building.indoors.Value;
            this.BuildingIndoors[building] = indoors;
            this.Add(indoors);
        }

        /// <summary>Add the given location.</summary>
        /// <param name="location">The location to add.</param>
        public void Add(GameLocation location)
        {
            if (location == null)
                return;

            // remove old location if needed
            this.Remove(location);

            // add location
            this.Added.Add(location);
            this.LocationDict[location] = new LocationTracker(location);

            // add buildings
            if (location is BuildableGameLocation buildableLocation)
                this.Add(buildableLocation.buildings);
        }

        /// <summary>Remove the given building.</summary>
        /// <param name="building">The building to remove.</param>
        public void Remove(Building building)
        {
            if (building == null)
                return;

            this.BuildingIndoors.Remove(building);
            this.Remove(building.indoors.Value);
        }

        /// <summary>Remove the given location.</summary>
        /// <param name="location">The location to remove.</param>
        public void Remove(GameLocation location)
        {
            if (location == null)
                return;

            if (this.LocationDict.TryGetValue(location, out LocationTracker watcher))
            {
                // track change
                this.Removed.Add(location);

                // remove
                this.LocationDict.Remove(location);
                watcher.Dispose();
                if (location is BuildableGameLocation buildableLocation)
                    this.Remove(buildableLocation.buildings);
            }
        }

        /****
        ** Helpers
        ****/
        /// <summary>The underlying watchers.</summary>
        private IEnumerable<IWatcher> GetWatchers()
        {
            yield return this.LocationListWatcher;
            yield return this.MineLocationListWatcher;
            foreach (LocationTracker watcher in this.Locations)
                yield return watcher;
        }
    }
}
