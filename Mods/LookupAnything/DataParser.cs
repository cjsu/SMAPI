using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.LookupAnything.Framework;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.Data;
using Pathoschild.Stardew.LookupAnything.Framework.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using SFarmer = StardewValley.Farmer;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.LookupAnything
{
    /// <summary>Parses the raw game data into usable models. These may be expensive operations and should be cached.</summary>
    internal class DataParser
    {
        /*********
        ** Fields
        *********/
        /// <summary>Provides utility methods for interacting with the game code.</summary>
        private readonly GameHelper GameHelper;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        public DataParser(GameHelper gameHelper)
        {
            this.GameHelper = gameHelper;
        }

        /// <summary>Read parsed data about the Community Center bundles.</summary>
        /// <remarks>Derived from the <see cref="StardewValley.Locations.CommunityCenter"/> constructor and <see cref="StardewValley.Menus.JunimoNoteMenu.openRewardsMenu"/>.</remarks>
        public IEnumerable<BundleModel> GetBundles()
        {
            IDictionary<string, string> data = Game1.content.Load<Dictionary<string, string>>("Data\\Bundles");
            foreach (var entry in data)
            {
                // parse key
                string[] keyParts = entry.Key.Split('/');
                string area = keyParts[0];
                int id = int.Parse(keyParts[1]);

                // parse bundle info
                string[] valueParts = entry.Value.Split('/');
                string name = valueParts[0];
                string reward = valueParts[1];
                string displayName = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en
                    ? name // field isn't present in English
                    : valueParts.Last(); // number of fields varies, but display name is always last

                // parse ingredients
                List<BundleIngredientModel> ingredients = new List<BundleIngredientModel>();
                string[] ingredientData = valueParts[2].Split(' ');
                for (int i = 0; i < ingredientData.Length; i += 3)
                {
                    int index = i / 3;
                    int itemID = int.Parse(ingredientData[i]);
                    int stack = int.Parse(ingredientData[i + 1]);
                    ItemQuality quality = (ItemQuality)int.Parse(ingredientData[i + 2]);
                    ingredients.Add(new BundleIngredientModel(index, itemID, stack, quality));
                }

                // create bundle
                yield return new BundleModel(id, name, displayName, area, reward, ingredients);
            }
        }

        /// <summary>Get parsed data about the friendship between a player and NPC.</summary>
        /// <param name="player">The player.</param>
        /// <param name="npc">The NPC.</param>
        /// <param name="friendship">The current friendship data.</param>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public FriendshipModel GetFriendshipForVillager(SFarmer player, NPC npc, Friendship friendship, Metadata metadata)
        {
            return new FriendshipModel(player, npc, friendship, metadata.Constants);
        }

        /// <summary>Get parsed data about the friendship between a player and NPC.</summary>
        /// <param name="player">The player.</param>
        /// <param name="pet">The pet.</param>
        public FriendshipModel GetFriendshipForPet(SFarmer player, Pet pet)
        {
            return new FriendshipModel(pet.friendshipTowardFarmer, Pet.maxFriendship / 10, Pet.maxFriendship);
        }

        /// <summary>Get parsed data about the friendship between a player and NPC.</summary>
        /// <param name="player">The player.</param>
        /// <param name="animal">The farm animal.</param>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public FriendshipModel GetFriendshipForAnimal(SFarmer player, FarmAnimal animal, Metadata metadata)
        {
            return new FriendshipModel(animal.friendshipTowardFarmer.Value, metadata.Constants.AnimalFriendshipPointsPerLevel, metadata.Constants.AnimalFriendshipMaxPoints);
        }

        /// <summary>Get the raw gift tastes from the underlying data.</summary>
        /// <param name="objects">The game's object data.</param>
        /// <remarks>Reverse engineered from <c>Data\NPCGiftTastes</c> and <see cref="StardewValley.NPC.getGiftTasteForThisItem"/>.</remarks>
        public IEnumerable<GiftTasteModel> GetGiftTastes(ObjectModel[] objects)
        {
            // extract raw values
            var tastes = new List<GiftTasteModel>();
            {
                // define data schema
                var universal = new Dictionary<string, GiftTaste>
                {
                    ["Universal_Love"] = GiftTaste.Love,
                    ["Universal_Like"] = GiftTaste.Like,
                    ["Universal_Neutral"] = GiftTaste.Neutral,
                    ["Universal_Dislike"] = GiftTaste.Dislike,
                    ["Universal_Hate"] = GiftTaste.Hate
                };
                var personalMetadataKeys = new Dictionary<int, GiftTaste>
                {
                    // metadata is paired: odd values contain a list of item references, even values contain the reaction dialogue
                    [1] = GiftTaste.Love,
                    [3] = GiftTaste.Like,
                    [5] = GiftTaste.Dislike,
                    [7] = GiftTaste.Hate,
                    [9] = GiftTaste.Neutral
                };

                // read data
                IDictionary<string, string> data = Game1.NPCGiftTastes;
                foreach (string villager in data.Keys)
                {
                    string tasteStr = data[villager];

                    if (universal.ContainsKey(villager))
                    {
                        GiftTaste taste = universal[villager];
                        tastes.AddRange(
                            from refID in tasteStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            select new GiftTasteModel(taste, "*", int.Parse(refID), isUniversal: true)
                        );
                    }
                    else
                    {
                        string[] personalData = tasteStr.Split('/');
                        foreach (KeyValuePair<int, GiftTaste> taste in personalMetadataKeys)
                        {
                            tastes.AddRange(
                                from refID in
                                    personalData[taste.Key].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                select new GiftTasteModel(taste.Value, villager, int.Parse(refID))
                            );
                        }
                    }
                }
            }

            // get sanitised data
            HashSet<int> validItemIDs = new HashSet<int>(objects.Select(p => p.ParentSpriteIndex));
            HashSet<int> validCategories = new HashSet<int>(objects.Where(p => p.Category != 0).Select(p => p.Category));
            return tastes
                .Where(model => validCategories.Contains(model.RefID) || validItemIDs.Contains(model.RefID)); // ignore invalid entries
        }

        /// <summary>Parse monster data.</summary>
        /// <remarks>Reverse engineered from <see cref="StardewValley.Monsters.Monster.parseMonsterInfo"/>, <see cref="GameLocation.monsterDrop"/>, and the <see cref="Debris"/> constructor.</remarks>
        public IEnumerable<MonsterData> GetMonsters()
        {
            Dictionary<string, string> data = Game1.content.Load<Dictionary<string, string>>("Data\\Monsters");

            foreach (var entry in data)
            {
                // monster fields
                string[] fields = entry.Value.Split('/');
                string name = entry.Key;
                int health = int.Parse(fields[0]);
                int damageToFarmer = int.Parse(fields[1]);
                //int minCoins = int.Parse(fields[2]);
                //int maxCoins = int.Parse(fields[3]) + 1;
                bool isGlider = bool.Parse(fields[4]);
                int durationOfRandomMovements = int.Parse(fields[5]);
                int resilience = int.Parse(fields[7]);
                double jitteriness = double.Parse(fields[8]);
                int moveTowardsPlayerThreshold = int.Parse(fields[9]);
                int speed = int.Parse(fields[10]);
                double missChance = double.Parse(fields[11]);
                bool isMineMonster = bool.Parse(fields[12]);

                // drops
                var drops = new List<ItemDropData>();
                string[] dropFields = fields[6].Split(' ');
                for (int i = 0; i < dropFields.Length; i += 2)
                {
                    // get drop info
                    int itemID = int.Parse(dropFields[i]);
                    float chance = float.Parse(dropFields[i + 1]);
                    int maxDrops = 1;

                    // if itemID is negative, game randomly drops 1-3
                    if (itemID < 0)
                    {
                        itemID = -itemID;
                        maxDrops = 3;
                    }

                    // some item IDs have special meaning
                    if (itemID == Debris.copperDebris)
                        itemID = SObject.copper;
                    else if (itemID == Debris.ironDebris)
                        itemID = SObject.iron;
                    else if (itemID == Debris.coalDebris)
                        itemID = SObject.coal;
                    else if (itemID == Debris.goldDebris)
                        itemID = SObject.gold;
                    else if (itemID == Debris.coinsDebris)
                        continue; // no drop
                    else if (itemID == Debris.iridiumDebris)
                        itemID = SObject.iridium;
                    else if (itemID == Debris.woodDebris)
                        itemID = SObject.wood;
                    else if (itemID == Debris.stoneDebris)
                        itemID = SObject.stone;

                    // add drop
                    drops.Add(new ItemDropData(itemID, maxDrops, chance));
                }
                if (isMineMonster && Game1.player.timesReachedMineBottom >= 1)
                {
                    drops.Add(new ItemDropData(SObject.diamondIndex, 1, 0.008f));
                    drops.Add(new ItemDropData(SObject.prismaticShardIndex, 1, 0.008f));
                }

                // yield data
                yield return new MonsterData(
                    name: name,
                    health: health,
                    damageToFarmer: damageToFarmer,
                    isGlider: isGlider,
                    durationOfRandomMovements: durationOfRandomMovements,
                    resilience: resilience,
                    jitteriness: jitteriness,
                    moveTowardsPlayerThreshold: moveTowardsPlayerThreshold,
                    speed: speed,
                    missChance: missChance,
                    isMineMonster: isMineMonster,
                    drops: drops
                );
            }
        }

        /// <summary>Parse gift tastes.</summary>
        /// <param name="monitor">The monitor with which to log errors.</param>
        /// <remarks>Derived from the <see cref="CraftingRecipe.createItem"/>.</remarks>
        public IEnumerable<ObjectModel> GetObjects(IMonitor monitor)
        {
            IDictionary<int, string> data = Game1.objectInformation;

            foreach (var pair in data)
            {
                int parentSpriteIndex = pair.Key;

                ObjectModel model;
                try
                {

                    string[] fields = pair.Value.Split('/');

                    // ring
                    if (parentSpriteIndex >= Ring.ringLowerIndexRange && parentSpriteIndex <= Ring.ringUpperIndexRange)
                    {
                        model = new ObjectModel(
                            parentSpriteIndex: parentSpriteIndex,
                            name: fields[0],
                            description: fields[1],
                            price: int.Parse(fields[2]),
                            edibility: -300,
                            type: fields[3],
                            category: SObject.ringCategory
                        );
                    }

                    // any other object
                    else
                    {
                        string name = fields[SObject.objectInfoNameIndex];
                        int price = int.Parse(fields[SObject.objectInfoPriceIndex]);
                        int edibility = int.Parse(fields[SObject.objectInfoEdibilityIndex]);
                        string description = fields[SObject.objectInfoDescriptionIndex];

                        // type & category
                        string[] typeParts = fields[SObject.objectInfoTypeIndex].Split(' ');
                        string typeName = typeParts[0];
                        int category = 0;
                        if (typeParts.Length > 1)
                            category = int.Parse(typeParts[1]);

                        model = new ObjectModel(parentSpriteIndex, name, description, price, edibility, typeName, category);
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log($"Couldn't parse object #{parentSpriteIndex} from Content\\Data\\ObjectInformation.xnb due to an invalid format.\nObject data: {pair.Value}\nError: {ex}", LogLevel.Warn);
                    continue;
                }
                yield return model;
            }
        }

        /// <summary>Get the recipe ingredients.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        /// <param name="reflectionHelper">Simplifies access to private game code.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        public RecipeModel[] GetRecipes(Metadata metadata, IReflectionHelper reflectionHelper, ITranslationHelper translations)
        {
            List<RecipeModel> recipes = new List<RecipeModel>();

            // cooking recipes
            recipes.AddRange(
                from entry in CraftingRecipe.cookingRecipes
                let recipe = new CraftingRecipe(entry.Key, isCookingRecipe: true)
                select new RecipeModel(recipe, reflectionHelper, translations)
            );

            // crafting recipes
            recipes.AddRange(
                from entry in CraftingRecipe.craftingRecipes
                let recipe = new CraftingRecipe(entry.Key, isCookingRecipe: false)
                select new RecipeModel(recipe, reflectionHelper, translations)
            );

            // machine recipes
            recipes.AddRange(
                from entry in metadata.MachineRecipes
                let machine = new SObject(Vector2.Zero, entry.MachineID)
                select new RecipeModel(null, RecipeType.MachineInput, machine.DisplayName, entry.Ingredients, ingredient => this.CreateRecipeItem(ingredient.ParentSheetIndex, entry.Output), false, entry.ExceptIngredients, outputItemIndex: entry.Output)
            );

            // building recipes
            recipes.AddRange(
                from entry in metadata.BuildingRecipes
                let building = new BluePrint(entry.BuildingKey)
                select new RecipeModel(null, RecipeType.BuildingBlueprint, building.displayName, entry.Ingredients, ingredient => this.CreateRecipeItem(ingredient.ParentSheetIndex, entry.Output), false, entry.ExceptIngredients)
            );

            return recipes.ToArray();
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Create a custom recipe output.</summary>
        /// <param name="inputID">The input ingredient ID.</param>
        /// <param name="outputID">The output item ID.</param>
        private SObject CreateRecipeItem(int inputID, int outputID)
        {
            SObject item = this.GameHelper.GetObjectBySpriteIndex(outputID);
            switch (outputID)
            {
                case 342:
                    item.preserve.Value = SObject.PreserveType.Pickle;
                    item.preservedParentSheetIndex.Value = inputID;
                    break;
                case 344:
                    item.preserve.Value = SObject.PreserveType.Jelly;
                    item.preservedParentSheetIndex.Value = inputID;
                    break;
                case 348:
                    item.preserve.Value = SObject.PreserveType.Wine;
                    item.preservedParentSheetIndex.Value = inputID;
                    break;
                case 350:
                    item.preserve.Value = SObject.PreserveType.Juice;
                    item.preservedParentSheetIndex.Value = inputID;
                    break;
            }
            return item;
        }
    }
}
