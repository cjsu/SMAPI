#if !SMAPI_3_0_STRICT
using System;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="ControlEvents.KeyboardChanged"/> event.</summary>
    public class EventArgsKeyboardStateChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous keyboard state.</summary>
        public KeyboardState NewState { get; }

        /// <summary>The current keyboard state.</summary>
        public KeyboardState PriorState { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="priorState">The previous keyboard state.</param>
        /// <param name="newState">The current keyboard state.</param>
        public EventArgsKeyboardStateChanged(KeyboardState priorState, KeyboardState newState)
        {
            this.PriorState = priorState;
            this.NewState = newState;
        }
    }
}
#endif
