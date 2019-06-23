#if !SMAPI_3_0_STRICT
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="ControlEvents.ControllerTriggerPressed"/> event.</summary>
    public class EventArgsControllerTriggerPressed : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The player who pressed the button.</summary>
        public PlayerIndex PlayerIndex { get; }

        /// <summary>The controller button that was pressed.</summary>
        public Buttons ButtonPressed { get; }

        /// <summary>The current trigger value.</summary>
        public float Value { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="playerIndex">The player who pressed the trigger button.</param>
        /// <param name="button">The trigger button that was pressed.</param>
        /// <param name="value">The current trigger value.</param>
        public EventArgsControllerTriggerPressed(PlayerIndex playerIndex, Buttons button, float value)
        {
            this.PlayerIndex = playerIndex;
            this.ButtonPressed = button;
            this.Value = value;
        }
    }
}
#endif
