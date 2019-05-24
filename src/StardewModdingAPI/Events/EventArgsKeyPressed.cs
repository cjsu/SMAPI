#if !SMAPI_3_0_STRICT
using System;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="ControlEvents.KeyboardChanged"/> event.</summary>
    public class EventArgsKeyPressed : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The keyboard button that was pressed.</summary>
        public Keys KeyPressed { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="key">The keyboard button that was pressed.</param>
        public EventArgsKeyPressed(Keys key)
        {
            this.KeyPressed = key;
        }
    }
}
#endif
