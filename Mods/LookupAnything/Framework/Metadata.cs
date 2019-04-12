using System.Linq;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.Data;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework
{
    /// <summary>Provides metadata that's not available from the game data directly (e.g. because it's buried in the logic).</summary>
    internal class Metadata
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Constant values hardcoded by the game.</summary>
        public ConstantData Constants { get; set; }

        /// <summary>Metadata for game objects (including inventory items, terrain features, crops, trees, and other map objects).</summary>
        public ObjectData[] Objects { get; set; }

        /// <summary>Metadata for NPCs in the game.</summary>
        public CharacterData[] Characters { get; set; }

        /// <summary>Information about Adventure Guild monster-slaying quests.</summary>
        /// <remarks>Derived from <see cref="StardewValley.Locations.AdventureGuild.showMonsterKillList"/>.</remarks>
        public AdventureGuildQuestData[] AdventureGuildQuests { get; set; }

        /// <summary>The building recipes.</summary>
        /// <remarks>Derived from <see cref="StardewValley.Buildings.Mill.dayUpdate"/>.</remarks>
        public BuildingRecipeData[] BuildingRecipes { get; set; }

        /// <summary>The machine recipes.</summary>
        /// <remarks>Derived from <see cref="Object.performObjectDropInAction"/>.</remarks>
        public MachineRecipeData[] MachineRecipes { get; set; }

        /// <summary>The shops that buy items from the player.</summary>
        /// <remarks>Derived from <see cref="StardewValley.Menus.ShopMenu"/> constructor.</remarks>
        public ShopData[] Shops { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether the metadata seems to be basically valid.</summary>
        public bool LooksValid()
        {
            return new object[] { this.Constants, this.Objects, this.Characters, this.AdventureGuildQuests, this.BuildingRecipes, this.MachineRecipes, this.Shops }.All(p => p != null);
        }

        /// <summary>Get overrides for a game object.</summary>
        /// <param name="item">The item for which to get overrides.</param>
        /// <param name="context">The context for which to get an override.</param>
        public ObjectData GetObject(Item item, ObjectContext context)
        {
            ItemSpriteType sheet = item.GetSpriteType();
            return this.Objects
                .FirstOrDefault(obj => obj.SpriteSheet == sheet && obj.SpriteID.Contains(item.ParentSheetIndex) && obj.Context.HasFlag(context));
        }

        /// <summary>Get overrides for a game object.</summary>
        /// <param name="character">The character for which to get overrides.</param>
        /// <param name="type">The character type.</param>
        public CharacterData GetCharacter(NPC character, TargetType type)
        {
            return
                this.Characters?.FirstOrDefault(p => p.ID == $"{type}::{character.Name}") // override by type + name
                ?? this.Characters?.FirstOrDefault(p => p.ID == type.ToString()); // override by type
        }

        /// <summary>Get the adventurer guild quest for the specified monster (if any).</summary>
        /// <param name="monster">The monster name.</param>
        public AdventureGuildQuestData GetAdventurerGuildQuest(string monster)
        {
            return this.AdventureGuildQuests.FirstOrDefault(p => p.Targets.Contains(monster));
        }
    }
}
