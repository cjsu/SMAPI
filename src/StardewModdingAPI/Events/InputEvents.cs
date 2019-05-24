#if !SMAPI_3_0_STRICT
using System;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Events;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when the player uses a controller, keyboard, or mouse button.</summary>
    [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.Events) + " instead. See https://smapi.io/3.0 for more info.")]
    public static class InputEvents
    {
        /*********
        ** Fields
        *********/
        /// <summary>The core event manager.</summary>
        private static EventManager EventManager;


        /*********
        ** Events
        *********/
        /// <summary>Raised when the player presses a button on the keyboard, controller, or mouse.</summary>
        public static event EventHandler<EventArgsInput> ButtonPressed
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                InputEvents.EventManager.Legacy_ButtonPressed.Add(value);
            }
            remove => InputEvents.EventManager.Legacy_ButtonPressed.Remove(value);
        }

        /// <summary>Raised when the player releases a keyboard key on the keyboard, controller, or mouse.</summary>
        public static event EventHandler<EventArgsInput> ButtonReleased
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                InputEvents.EventManager.Legacy_ButtonReleased.Add(value);
            }
            remove => InputEvents.EventManager.Legacy_ButtonReleased.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the events.</summary>
        /// <param name="eventManager">The core event manager.</param>
        internal static void Init(EventManager eventManager)
        {
            InputEvents.EventManager = eventManager;
        }
    }
}
#endif
