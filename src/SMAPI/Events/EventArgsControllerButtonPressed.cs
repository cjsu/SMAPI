#if !SMAPI_3_0_STRICT
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="ControlEvents.ControllerButtonPressed"/> event.</summary>
    public class EventArgsControllerButtonPressed : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The player who pressed the button.</summary>
        public PlayerIndex PlayerIndex { get; }

        /// <summary>The controller button that was pressed.</summary>
        public Buttons ButtonPressed { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="playerIndex">The player who pressed the button.</param>
        /// <param name="button">The controller button that was pressed.</param>
        public EventArgsControllerButtonPressed(PlayerIndex playerIndex, Buttons button)
        {
            this.PlayerIndex = playerIndex;
            this.ButtonPressed = button;
        }
    }
}
#endif
