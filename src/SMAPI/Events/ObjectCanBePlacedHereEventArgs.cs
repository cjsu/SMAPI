using StardewValley;
using Microsoft.Xna.Framework;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for an <see cref="IHookEvents.ObjectCanBePlacedHere"/> event.</summary>
    public class ObjectCanBePlacedHereEventArgs : System.EventArgs
    {
        /*********
        ** Accessors
        *********/
        public Object __instance { get; }

        public GameLocation location { get; }

        public Vector2 tile { get; }

        public bool __result;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="oldSize">The previous window size.</param>
        /// <param name="newSize">The current window size.</param>
        internal ObjectCanBePlacedHereEventArgs(Object __instance, GameLocation location, Vector2 tile, bool __result)
        {
            this.__instance = __instance;
            this.location = location;
            this.tile = tile;
            this.__result = __result;
        }
    }
}
