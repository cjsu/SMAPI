#if SMAPI_1_x
using System;
using SFarmer = StardewValley.Farmer;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="PlayerEvents.FarmerChanged"/> event.</summary>
    public class EventArgsFarmerChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous player character.</summary>
        public SFarmer NewFarmer { get; }

        /// <summary>The new player character.</summary>
        public SFarmer PriorFarmer { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="priorFarmer">The previous player character.</param>
        /// <param name="newFarmer">The new player character.</param>
        public EventArgsFarmerChanged(SFarmer priorFarmer, SFarmer newFarmer)
        {
            this.PriorFarmer = priorFarmer;
            this.NewFarmer = newFarmer;
        }
    }
}
#endif