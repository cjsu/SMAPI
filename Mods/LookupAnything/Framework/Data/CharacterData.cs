namespace Pathoschild.Stardew.LookupAnything.Framework.Data
{
    /// <summary>Provides override metadata about a game NPC.</summary>
    internal class CharacterData
    {
        /*********
        ** Accessors
        *********/
        /****
        ** Identify object
        ****/
        /// <summary>The NPC identifier, like "Horse" (any NPCs of type Horse) or "Villager::Gunther" (any NPCs of type Villager with the name "Gunther").</summary>
        public string ID { get; set; }


        /****
        ** Overrides
        ****/
        /// <summary>The translation key which should override the NPC description (if any).</summary>
        public string DescriptionKey { get; set; }
    }
}
