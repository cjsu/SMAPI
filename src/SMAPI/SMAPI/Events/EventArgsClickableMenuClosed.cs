#if !SMAPI_3_0_STRICT
using System;
using StardewValley.Menus;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="MenuEvents.MenuClosed"/> event.</summary>
    public class EventArgsClickableMenuClosed : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The menu that was closed.</summary>
        public IClickableMenu PriorMenu { get; }


        /*********
        ** Accessors
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="priorMenu">The menu that was closed.</param>
        public EventArgsClickableMenuClosed(IClickableMenu priorMenu)
        {
            this.PriorMenu = priorMenu;
        }
    }
}
#endif
