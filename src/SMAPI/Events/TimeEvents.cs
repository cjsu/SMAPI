#if !SMAPI_3_0_STRICT
using System;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Events;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when the in-game date or time changes.</summary>
    [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.Events) + " instead. See https://smapi.io/3.0 for more info.")]
    public static class TimeEvents
    {
        /*********
        ** Fields
        *********/
        /// <summary>The core event manager.</summary>
        private static EventManager EventManager;


        /*********
        ** Events
        *********/
        /// <summary>Raised after the game begins a new day, including when loading a save.</summary>
        public static event EventHandler AfterDayStarted
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                TimeEvents.EventManager.Legacy_AfterDayStarted.Add(value);
            }
            remove => TimeEvents.EventManager.Legacy_AfterDayStarted.Remove(value);
        }

        /// <summary>Raised after the in-game clock changes.</summary>
        public static event EventHandler<EventArgsIntChanged> TimeOfDayChanged
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                TimeEvents.EventManager.Legacy_TimeOfDayChanged.Add(value);
            }
            remove => TimeEvents.EventManager.Legacy_TimeOfDayChanged.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the events.</summary>
        /// <param name="eventManager">The core event manager.</param>
        internal static void Init(EventManager eventManager)
        {
            TimeEvents.EventManager = eventManager;
        }
    }
}
#endif
