#if !SMAPI_3_0_STRICT
using System;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Events;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when something happens in the mines.</summary>
    [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.Events) + " instead. See https://smapi.io/3.0 for more info.")]
    public static class MineEvents
    {
        /*********
        ** Fields
        *********/
        /// <summary>The core event manager.</summary>
        private static EventManager EventManager;


        /*********
        ** Events
        *********/
        /// <summary>Raised after the player warps to a new level of the mine.</summary>
        public static event EventHandler<EventArgsMineLevelChanged> MineLevelChanged
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                MineEvents.EventManager.Legacy_MineLevelChanged.Add(value);
            }
            remove => MineEvents.EventManager.Legacy_MineLevelChanged.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the events.</summary>
        /// <param name="eventManager">The core event manager.</param>
        internal static void Init(EventManager eventManager)
        {
            MineEvents.EventManager = eventManager;
        }
    }
}
#endif
