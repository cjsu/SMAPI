#if !SMAPI_3_0_STRICT
using System;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="MineEvents.MineLevelChanged"/> event.</summary>
    public class EventArgsMineLevelChanged : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous mine level.</summary>
        public int PreviousMineLevel { get; }

        /// <summary>The current mine level.</summary>
        public int CurrentMineLevel { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="previousMineLevel">The previous mine level.</param>
        /// <param name="currentMineLevel">The current mine level.</param>
        public EventArgsMineLevelChanged(int previousMineLevel, int currentMineLevel)
        {
            this.PreviousMineLevel = previousMineLevel;
            this.CurrentMineLevel = currentMineLevel;
        }
    }
}
#endif
