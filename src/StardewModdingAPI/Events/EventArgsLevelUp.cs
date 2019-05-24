#if !SMAPI_3_0_STRICT
using System;
using StardewModdingAPI.Enums;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments for a <see cref="PlayerEvents.LeveledUp"/> event.</summary>
    public class EventArgsLevelUp : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The player skill that leveled up.</summary>
        public LevelType Type { get; }

        /// <summary>The new skill level.</summary>
        public int NewLevel { get; }

        /// <summary>The player skill types.</summary>
        public enum LevelType
        {
            /// <summary>The combat skill.</summary>
            Combat = SkillType.Combat,

            /// <summary>The farming skill.</summary>
            Farming = SkillType.Farming,

            /// <summary>The fishing skill.</summary>
            Fishing = SkillType.Fishing,

            /// <summary>The foraging skill.</summary>
            Foraging = SkillType.Foraging,

            /// <summary>The mining skill.</summary>
            Mining = SkillType.Mining,

            /// <summary>The luck skill.</summary>
            Luck = SkillType.Luck
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The player skill that leveled up.</param>
        /// <param name="newLevel">The new skill level.</param>
        public EventArgsLevelUp(LevelType type, int newLevel)
        {
            this.Type = type;
            this.NewLevel = newLevel;
        }
    }
}
#endif
