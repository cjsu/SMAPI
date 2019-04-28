#if !SMAPI_3_0_STRICT
using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="LocationEvents.LocationsChanged"/> event.</summary>
    public class EventArgsLocationsChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The added locations.</summary>
        public IEnumerable<GameLocation> Added { get; }

        /// <summary>The removed locations.</summary>
        public IEnumerable<GameLocation> Removed { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="added">The added locations.</param>
        /// <param name="removed">The removed locations.</param>
        public EventArgsLocationsChanged(IEnumerable<GameLocation> added, IEnumerable<GameLocation> removed)
        {
            this.Added = added.ToArray();
            this.Removed = removed.ToArray();
        }
    }
}
#endif
