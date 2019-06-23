using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.Input;
using StardewModdingAPI.Framework.StateTracking;
using StardewModdingAPI.Framework.StateTracking.FieldWatchers;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework
{
    /// <summary>Monitors the entire game state for changes, virally spreading watchers into any new entities that get created.</summary>
    internal class WatcherCore
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying watchers for convenience. These are accessible individually as separate properties.</summary>
        private readonly List<IWatcher> Watchers = new List<IWatcher>();


        /*********
        ** Accessors
        *********/
        /// <summary>Tracks changes to the window size.</summary>
        public readonly IValueWatcher<Point> WindowSizeWatcher;

        /// <summary>Tracks changes to the current player.</summary>
        public PlayerTracker CurrentPlayerTracker;

        /// <summary>Tracks changes to the time of day (in 24-hour military format).</summary>
        public readonly IValueWatcher<int> TimeWatcher;

        /// <summary>Tracks changes to the save ID.</summary>
        public readonly IValueWatcher<ulong> SaveIdWatcher;

        /// <summary>Tracks changes to the game's locations.</summary>
        public readonly WorldLocationsTracker LocationsWatcher;

        /// <summary>Tracks changes to <see cref="Game1.activeClickableMenu"/>.</summary>
        public readonly IValueWatcher<IClickableMenu> ActiveMenuWatcher;

        /// <summary>Tracks changes to the cursor position.</summary>
        public readonly IValueWatcher<ICursorPosition> CursorWatcher;

        /// <summary>Tracks changes to the mouse wheel scroll.</summary>
        public readonly IValueWatcher<int> MouseWheelScrollWatcher;

        /// <summary>Tracks changes to the content locale.</summary>
        public readonly IValueWatcher<LocalizedContentManager.LanguageCode> LocaleWatcher;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="inputState">Manages input visible to the game.</param>
        public WatcherCore(SInputState inputState)
        {
            // init watchers
            this.CursorWatcher = WatcherFactory.ForEquatable(() => inputState.CursorPosition);
            this.MouseWheelScrollWatcher = WatcherFactory.ForEquatable(() => inputState.RealMouse.ScrollWheelValue);
            this.SaveIdWatcher = WatcherFactory.ForEquatable(() => Game1.hasLoadedGame ? Game1.uniqueIDForThisGame : 0);
            this.WindowSizeWatcher = WatcherFactory.ForEquatable(() => new Point(Game1.viewport.Width, Game1.viewport.Height));
            this.TimeWatcher = WatcherFactory.ForEquatable(() => Game1.timeOfDay);
            this.ActiveMenuWatcher = WatcherFactory.ForReference(() => Game1.activeClickableMenu);
            this.LocationsWatcher = new WorldLocationsTracker((ObservableCollection<GameLocation>)Game1.locations, MineShaft.activeMines);
            this.LocaleWatcher = WatcherFactory.ForGenericEquality(() => LocalizedContentManager.CurrentLanguageCode);
            this.Watchers.AddRange(new IWatcher[]
            {
                this.CursorWatcher,
                this.MouseWheelScrollWatcher,
                this.SaveIdWatcher,
                this.WindowSizeWatcher,
                this.TimeWatcher,
                this.ActiveMenuWatcher,
                this.LocationsWatcher,
                this.LocaleWatcher
            });
        }

        /// <summary>Update the watchers and adjust for added or removed entities.</summary>
        public void Update()
        {
            // reset player
            if (Context.IsWorldReady)
            {
                if (this.CurrentPlayerTracker == null || this.CurrentPlayerTracker.Player != Game1.player)
                {
                    this.CurrentPlayerTracker?.Dispose();
                    this.CurrentPlayerTracker = new PlayerTracker(Game1.player);
                }
            }
            else
            {
                if (this.CurrentPlayerTracker != null)
                {
                    this.CurrentPlayerTracker.Dispose();
                    this.CurrentPlayerTracker = null;
                }
            }

            // update values
            foreach (IWatcher watcher in this.Watchers)
                watcher.Update();
            this.CurrentPlayerTracker?.Update();
            this.LocationsWatcher.Update();
        }

        /// <summary>Reset the current values as the baseline.</summary>
        public void Reset()
        {
            foreach (IWatcher watcher in this.Watchers)
                watcher.Reset();
            this.CurrentPlayerTracker?.Reset();
            this.LocationsWatcher.Reset();
        }
    }
}
