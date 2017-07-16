#if SMAPI_1_x
using System;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a string field that changed value.</summary>
    public class EventArgsStringChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous value.</summary>
        public string NewString { get; }

        /// <summary>The current value.</summary>
        public string PriorString { get; }

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="priorString">The previous value.</param>
        /// <param name="newString">The current value.</param>
        public EventArgsStringChanged(string priorString, string newString)
        {
            this.NewString = newString;
            this.PriorString = priorString;
        }
    }
}
#endif