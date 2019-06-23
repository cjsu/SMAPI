#if !SMAPI_3_0_STRICT
using System;
using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="PlayerEvents.Warped"/> event.</summary>
    public class EventArgsPlayerWarped : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The player's previous location.</summary>
        public GameLocation PriorLocation { get; }

        /// <summary>The player's current location.</summary>
        public GameLocation NewLocation { get; }



        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="priorLocation">The player's previous location.</param>
        /// <param name="newLocation">The player's current location.</param>
        public EventArgsPlayerWarped(GameLocation priorLocation, GameLocation newLocation)
        {
            this.NewLocation = newLocation;
            this.PriorLocation = priorLocation;
        }
    }
}
#endif
