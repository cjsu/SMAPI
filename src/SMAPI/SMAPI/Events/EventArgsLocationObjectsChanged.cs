#if !SMAPI_3_0_STRICT
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="LocationEvents.ObjectsChanged"/> event.</summary>
    public class EventArgsLocationObjectsChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The location which changed.</summary>
        public GameLocation Location { get; }

        /// <summary>The objects added to the location.</summary>
        public IEnumerable<KeyValuePair<Vector2, SObject>> Added { get; }

        /// <summary>The objects removed from the location.</summary>
        public IEnumerable<KeyValuePair<Vector2, SObject>> Removed { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="location">The location which changed.</param>
        /// <param name="added">The objects added to the location.</param>
        /// <param name="removed">The objects removed from the location.</param>
        public EventArgsLocationObjectsChanged(GameLocation location, IEnumerable<KeyValuePair<Vector2, SObject>> added, IEnumerable<KeyValuePair<Vector2, SObject>> removed)
        {
            this.Location = location;
            this.Added = added.ToArray();
            this.Removed = removed.ToArray();
        }
    }
}
#endif
