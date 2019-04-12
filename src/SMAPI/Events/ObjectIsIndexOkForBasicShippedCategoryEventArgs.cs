using System;
using Microsoft.Xna.Framework;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for an <see cref="IHookEvents.ObjectIsIndexOkForBasicShippedCategoryEventArgs"/> event.</summary>
    public class ObjectIsIndexOkForBasicShippedCategoryEventArgs : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The index</summary>
        public int index { get; }

        public bool __result;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="oldSize">The previous window size.</param>
        /// <param name="newSize">The current window size.</param>
        internal ObjectIsIndexOkForBasicShippedCategoryEventArgs(int index, bool __result)
        {
            this.index = index;
            this.__result = __result;
        }
    }
}
