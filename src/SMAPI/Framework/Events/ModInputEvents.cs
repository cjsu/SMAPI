using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>Events raised when the player provides input using a controller, keyboard, or mouse.</summary>
    internal class ModInputEvents : ModEventsBase, IInputEvents
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        public event EventHandler<ButtonPressedEventArgs> ButtonPressed
        {
            add => this.EventManager.ButtonPressed.Add(value);
            remove => this.EventManager.ButtonPressed.Remove(value);
        }

        /// <summary>Raised after the player releases a button on the keyboard, controller, or mouse.</summary>
        public event EventHandler<ButtonReleasedEventArgs> ButtonReleased
        {
            add => this.EventManager.ButtonReleased.Add(value);
            remove => this.EventManager.ButtonReleased.Remove(value);
        }

        /// <summary>Raised after the player moves the in-game cursor.</summary>
        public event EventHandler<CursorMovedEventArgs> CursorMoved
        {
            add => this.EventManager.CursorMoved.Add(value);
            remove => this.EventManager.CursorMoved.Remove(value);
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        public event EventHandler<MouseWheelScrolledEventArgs> MouseWheelScrolled
        {
            add => this.EventManager.MouseWheelScrolled.Add(value);
            remove => this.EventManager.MouseWheelScrolled.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModInputEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
