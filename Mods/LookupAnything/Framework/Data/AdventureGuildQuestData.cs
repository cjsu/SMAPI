namespace Pathoschild.Stardew.LookupAnything.Framework.Data
{
    /// <summary>Information about an Adventure Guild monster-slaying quest.</summary>
    internal class AdventureGuildQuestData
    {
        /// <summary>The names of the monsters in this category.</summary>
        public string[] Targets { get; set; }

        /// <summary>The number of kills required for the reward.</summary>
        public int RequiredKills { get; set; }
    }
}
