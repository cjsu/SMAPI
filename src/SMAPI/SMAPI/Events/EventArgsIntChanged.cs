#if !SMAPI_3_0_STRICT
using System;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for an integer field that changed value.</summary>
    public class EventArgsIntChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous value.</summary>
        public int PriorInt { get; }

        /// <summary>The current value.</summary>
        public int NewInt { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="priorInt">The previous value.</param>
        /// <param name="newInt">The current value.</param>
        public EventArgsIntChanged(int priorInt, int newInt)
        {
            this.PriorInt = priorInt;
            this.NewInt = newInt;
        }
    }
}
#endif
