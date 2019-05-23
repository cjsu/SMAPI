#if !SMAPI_3_0_STRICT
using System;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Events;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised during the game's draw loop, when the game is rendering content to the window.</summary>
    [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.Events) + " instead. See https://smapi.io/3.0 for more info.")]
    public static class GraphicsEvents
    {
        /*********
        ** Fields
        *********/
        /// <summary>The core event manager.</summary>
        private static EventManager EventManager;


        /*********
        ** Events
        *********/
        /// <summary>Raised after the game window is resized.</summary>
        public static event EventHandler Resize
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GraphicsEvents.EventManager.Legacy_Resize.Add(value);
            }
            remove => GraphicsEvents.EventManager.Legacy_Resize.Remove(value);
        }

        /****
        ** Main render events
        ****/
        /// <summary>Raised before drawing the world to the screen.</summary>
        public static event EventHandler OnPreRenderEvent
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GraphicsEvents.EventManager.Legacy_OnPreRenderEvent.Add(value);
            }
            remove => GraphicsEvents.EventManager.Legacy_OnPreRenderEvent.Remove(value);
        }

        /// <summary>Raised after drawing the world to the screen.</summary>
        public static event EventHandler OnPostRenderEvent
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GraphicsEvents.EventManager.Legacy_OnPostRenderEvent.Add(value);
            }
            remove => GraphicsEvents.EventManager.Legacy_OnPostRenderEvent.Remove(value);
        }

        /****
        ** HUD events
        ****/
        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The HUD is available at this point, but not necessarily visible. (For example, the event is raised even if a menu is open.)</summary>
        public static event EventHandler OnPreRenderHudEvent
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GraphicsEvents.EventManager.Legacy_OnPreRenderHudEvent.Add(value);
            }
            remove => GraphicsEvents.EventManager.Legacy_OnPreRenderHudEvent.Remove(value);
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the screen. The HUD is available at this point, but not necessarily visible. (For example, the event is raised even if a menu is open.)</summary>
        public static event EventHandler OnPostRenderHudEvent
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GraphicsEvents.EventManager.Legacy_OnPostRenderHudEvent.Add(value);
            }
            remove => GraphicsEvents.EventManager.Legacy_OnPostRenderHudEvent.Remove(value);
        }

        /****
        ** GUI events
        ****/
        /// <summary>Raised before drawing a menu to the screen during a draw loop. This includes the game's internal menus like the title screen.</summary>
        public static event EventHandler OnPreRenderGuiEvent
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GraphicsEvents.EventManager.Legacy_OnPreRenderGuiEvent.Add(value);
            }
            remove => GraphicsEvents.EventManager.Legacy_OnPreRenderGuiEvent.Remove(value);
        }

        /// <summary>Raised after drawing a menu to the screen during a draw loop. This includes the game's internal menus like the title screen.</summary>
        public static event EventHandler OnPostRenderGuiEvent
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GraphicsEvents.EventManager.Legacy_OnPostRenderGuiEvent.Add(value);
            }
            remove => GraphicsEvents.EventManager.Legacy_OnPostRenderGuiEvent.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the events.</summary>
        /// <param name="eventManager">The core event manager.</param>
        internal static void Init(EventManager eventManager)
        {
            GraphicsEvents.EventManager = eventManager;
        }
    }
}
#endif
