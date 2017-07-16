#if SMAPI_1_x
using System;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="PlayerEvents.LoadedGame"/> event.</summary>
    public class EventArgsLoadedGameChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether the save has been loaded. This is always true.</summary>
        public bool LoadedGame { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="loaded">Whether the save has been loaded. This is always true.</param>
        public EventArgsLoadedGameChanged(bool loaded)
        {
            this.LoadedGame = loaded;
        }
    }
}
#endif