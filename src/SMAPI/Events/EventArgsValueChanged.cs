#if !SMAPI_3_0_STRICT
using System;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a field that changed value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public class EventArgsValueChanged<T> : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous value.</summary>
        public T PriorValue { get; }

        /// <summary>The current value.</summary>
        public T NewValue { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="priorValue">The previous value.</param>
        /// <param name="newValue">The current value.</param>
        public EventArgsValueChanged(T priorValue, T newValue)
        {
            this.PriorValue = priorValue;
            this.NewValue = newValue;
        }
    }
}
#endif
