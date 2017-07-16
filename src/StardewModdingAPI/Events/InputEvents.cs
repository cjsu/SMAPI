#if !SMAPI_1_x
using System;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Utilities;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when the player uses a controller, keyboard, or mouse button.</summary>
    public static class InputEvents
    {
        /*********
        ** Events
        *********/
        /// <summary>Raised when the player presses a button on the keyboard, controller, or mouse.</summary>
        public static event EventHandler<EventArgsInput> ButtonPressed;

        /// <summary>Raised when the player releases a keyboard key on the keyboard, controller, or mouse.</summary>
        public static event EventHandler<EventArgsInput> ButtonReleased;


        /*********
        ** Internal methods
        *********/
        /// <summary>Raise a <see cref="ButtonPressed"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="button">The button on the controller, keyboard, or mouse.</param>
        /// <param name="cursor">The cursor position.</param>
        /// <param name="isClick">Whether the input is considered a 'click' by the game for enabling action.</param>
        internal static void InvokeButtonPressed(IMonitor monitor, SButton button, ICursorPosition cursor, bool isClick)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(InputEvents)}.{nameof(InputEvents.ButtonPressed)}", InputEvents.ButtonPressed?.GetInvocationList(), null, new EventArgsInput(button, cursor, isClick));
        }

        /// <summary>Raise a <see cref="ButtonReleased"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="button">The button on the controller, keyboard, or mouse.</param>
        /// <param name="cursor">The cursor position.</param>
        /// <param name="isClick">Whether the input is considered a 'click' by the game for enabling action.</param>
        internal static void InvokeButtonReleased(IMonitor monitor, SButton button, ICursorPosition cursor, bool isClick)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(InputEvents)}.{nameof(InputEvents.ButtonReleased)}", InputEvents.ButtonReleased?.GetInvocationList(), null, new EventArgsInput(button, cursor, isClick));
        }
    }
}
#endif