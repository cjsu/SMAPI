using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>Events raised when the player provides input using a controller, keyboard, or mouse.</summary>
    internal class ModHookEvents : ModEventsBase, IHookEvents
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        public event Func<ObjectCanBePlacedHereEventArgs, bool> ObjectCanBePlacedHere
        {
            add => this.EventManager.ObjectCanBePlacedHere.Add(value);
            remove => this.EventManager.ObjectCanBePlacedHere.Remove(value);
        }

        /// <summary>Raised after the player releases a button on the keyboard, controller, or mouse.</summary>
        public event Func<ObjectCheckForActionEventArgs, bool> ObjectCheckForAction
        {
            add => this.EventManager.ObjectCheckForAction.Add(value);
            remove => this.EventManager.ObjectCheckForAction.Remove(value);
        }

        /// <summary>Raised after the player moves the in-game cursor.</summary>
        public event Func<ObjectIsIndexOkForBasicShippedCategoryEventArgs, bool> ObjectIsIndexOkForBasicShippedCategory
        {
            add => this.EventManager.ObjectIsIndexOkForBasicShippedCategory.Add(value);
            remove => this.EventManager.ObjectIsIndexOkForBasicShippedCategory.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod which uses this instance.</param>
        /// <param name="eventManager">The underlying event manager.</param>
        internal ModHookEvents(IModMetadata mod, EventManager eventManager)
            : base(mod, eventManager) { }
    }
}
