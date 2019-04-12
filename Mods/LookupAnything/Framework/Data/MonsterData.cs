using System.Collections.Generic;
using System.Linq;

namespace Pathoschild.Stardew.LookupAnything.Framework.Data
{
    /// <summary>A monster entry parsed from the game data.</summary>
    internal class MonsterData
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The monster name.</summary>
        public string Name { get; }

        /// <summary>The monster's health points.</summary>
        public int Health { get; }

        /// <summary>The damage points the monster afflicts on the player.</summary>
        public int DamageToFarmer { get; }

        /// <summary>Whether the monster can fly.</summary>
        public bool IsGlider { get; }

        /// <summary>The amount of time between random movement changes (in milliseconds).</summary>
        public int DurationOfRandomMovements { get; }

        /// <summary>The monster's damage resistance. (This amount is subtracted from received damage points.)</summary>
        public int Resilience { get; }

        /// <summary>The probability that a monster will randomly change direction when checked.</summary>
        public double Jitteriness { get; }

        /// <summary>The tile distance within which the monster will begin moving towards the player.</summary>
        public int MoveTowardsPlayerThreshold { get; }

        /// <summary>The speed at which the monster moves.</summary>
        public int Speed { get; }

        /// <summary>The probability that the player will miss when attacking this monster.</summary>
        public double MissChance { get; }

        /// <summary>Whether the monster appears in the mines. If <c>true</c>, the monster's base stats are increased once the player has reached the bottom of the mine at least once.</summary>
        public bool IsMineMonster { get; }

        /// <summary>The items dropped by this monster and their probability to drop.</summary>
        public ItemDropData[] Drops { get; }


        /*********
        ** public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">The monster name.</param>
        /// <param name="health">The monster's health points.</param>
        /// <param name="damageToFarmer">The damage points the monster afflicts on the player.</param>
        /// <summary>Whether the monster can fly.</summary>
        /// <param name="isGlider">The amount of time between random movement changes (in milliseconds).</param>
        /// <param name="durationOfRandomMovements">The amount of time between random movement changes (in milliseconds).</param>
        /// <param name="resilience">The monster's damage resistance.</param>
        /// <param name="jitteriness">The probability that a monster will randomly change direction when checked.</param>
        /// <param name="moveTowardsPlayerThreshold">The tile distance within which the monster will begin moving towards the player.</param>
        /// <param name="speed">The speed at which the monster moves.</param>
        /// <param name="missChance">The probability that the player will miss when attacking this monster.</param>
        /// <param name="isMineMonster">Whether the monster appears in the mines.</param>
        /// <param name="drops">The items dropped by this monster and their probability to drop.</param>
        public MonsterData(string name, int health, int damageToFarmer, bool isGlider, int durationOfRandomMovements, int resilience, double jitteriness, int moveTowardsPlayerThreshold, int speed, double missChance, bool isMineMonster, IEnumerable<ItemDropData> drops)
        {
            this.Name = name;
            this.Health = health;
            this.DamageToFarmer = damageToFarmer;
            this.IsGlider = isGlider;
            this.DurationOfRandomMovements = durationOfRandomMovements;
            this.Resilience = resilience;
            this.Jitteriness = jitteriness;
            this.MoveTowardsPlayerThreshold = moveTowardsPlayerThreshold;
            this.Speed = speed;
            this.MissChance = missChance;
            this.IsMineMonster = isMineMonster;
            this.Drops = drops.ToArray();
        }
    }
}
