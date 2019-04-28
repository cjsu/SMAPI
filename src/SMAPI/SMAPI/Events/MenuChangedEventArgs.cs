using System;
using StardewValley.Menus;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for an <see cref="IDisplayEvents.MenuChanged"/> event.</summary>
    public class MenuChangedEventArgs : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous menu.</summary>
        public IClickableMenu OldMenu { get; }

        /// <summary>The current menu.</summary>
        public IClickableMenu NewMenu { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="oldMenu">The previous menu.</param>
        /// <param name="newMenu">The current menu.</param>
        internal MenuChangedEventArgs(IClickableMenu oldMenu, IClickableMenu newMenu)
        {
            this.OldMenu = oldMenu;
            this.NewMenu = newMenu;
        }
    }
}
