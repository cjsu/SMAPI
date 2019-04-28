#if !SMAPI_3_0_STRICT
using System;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Events;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when a game menu is opened or closed (including internal menus like the title screen).</summary>
    [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.Events) + " instead. See https://smapi.io/3.0 for more info.")]
    public static class MenuEvents
    {
        /*********
        ** Fields
        *********/
        /// <summary>The core event manager.</summary>
        private static EventManager EventManager;


        /*********
        ** Events
        *********/
        /// <summary>Raised after a game menu is opened or replaced with another menu. This event is not invoked when a menu is closed.</summary>
        public static event EventHandler<EventArgsClickableMenuChanged> MenuChanged
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                MenuEvents.EventManager.Legacy_MenuChanged.Add(value);
            }
            remove => MenuEvents.EventManager.Legacy_MenuChanged.Remove(value);
        }

        /// <summary>Raised after a game menu is closed.</summary>
        public static event EventHandler<EventArgsClickableMenuClosed> MenuClosed
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                MenuEvents.EventManager.Legacy_MenuClosed.Add(value);
            }
            remove => MenuEvents.EventManager.Legacy_MenuClosed.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the events.</summary>
        /// <param name="eventManager">The core event manager.</param>
        internal static void Init(EventManager eventManager)
        {
            MenuEvents.EventManager = eventManager;
        }
    }
}
#endif
