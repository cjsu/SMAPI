using StardewValley;
using Microsoft.Xna.Framework;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for an <see cref="IHookEvents.ObjectCheckForAction"/> event.</summary>
    public class ObjectCheckForActionEventArgs : System.EventArgs
    {
        /*********
        ** Accessors
        *********/
        public Object __instance { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        internal ObjectCheckForActionEventArgs(Object __instance)
        {
            this.__instance = __instance;
        }
    }
}
