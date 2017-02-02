using System;
using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="LocationEvents.CurrentLocationChanged"/> event.</summary>
    public class EventArgsCurrentLocationChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The player's current location.</summary>
        public GameLocation NewLocation { get; private set; }

        /// <summary>The player's previous location.</summary>
        public GameLocation PriorLocation { get; private set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="priorLocation">The player's previous location.</param>
        /// <param name="newLocation">The player's current location.</param>
        public EventArgsCurrentLocationChanged(GameLocation priorLocation, GameLocation newLocation)
        {
            this.NewLocation = newLocation;
            this.PriorLocation = priorLocation;
        }
    }
}
