using System;
using System.Collections.Generic;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using StardewModdingAPI;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Models
{
    /// <summary>Represents metadata about a recipe.</summary>
    internal class RecipeModel
    {
        /*********
        ** Fields
        *********/
        /// <summary>The item that be created by this recipe, given the ingredient.</summary>
        private readonly Func<Item, Item> Item;


        /*********
        ** Accessors
        *********/
        /// <summary>The recipe's lookup name (if any).</summary>
        public string Key { get; }

        /// <summary>The recipe type.</summary>
        public RecipeType Type { get; }

        /// <summary>The display name for the machine or building name for which the recipe applies.</summary>
        public string DisplayType { get; }

        /// <summary>The items needed to craft the recipe (item ID => number needed).</summary>
        public IDictionary<int, int> Ingredients { get; }

        /// <summary>The item ID produced by this recipe, if applicable.</summary>
        public int? OutputItemIndex { get; }

        /// <summary>The ingredients which can't be used in this recipe (typically exceptions for a category ingredient).</summary>
        public int[] ExceptIngredients { get; }

        /// <summary>Whether the recipe must be learned before it can be used.</summary>
        public bool MustBeLearned { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="recipe">The recipe to parse.</param>
        /// <param name="reflectionHelper">Simplifies access to private game code.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        public RecipeModel(CraftingRecipe recipe, IReflectionHelper reflectionHelper, ITranslationHelper translations)
            : this(
                key: recipe.name,
                type: recipe.isCookingRecipe ? RecipeType.Cooking : RecipeType.Crafting,
                displayType: translations.Get(recipe.isCookingRecipe ? L10n.RecipeTypes.Cooking : L10n.RecipeTypes.Crafting),
                ingredients: reflectionHelper.GetField<Dictionary<int, int>>(recipe, "recipeList").GetValue(),
                item: item => recipe.createItem(),
                mustBeLearned: true,
                outputItemIndex: reflectionHelper.GetField<List<int>>(recipe, "itemToProduce").GetValue()[0]
            )
        { }

        /// <summary>Construct an instance.</summary>
        /// <param name="key">The recipe's lookup name (if any).</param>
        /// <param name="type">The recipe type.</param>
        /// <param name="displayType">The display name for the machine or building name for which the recipe applies.</param>
        /// <param name="ingredients">The items needed to craft the recipe (item ID => number needed).</param>
        /// <param name="item">The item that be created by this recipe.</param>
        /// <param name="mustBeLearned">Whether the recipe must be learned before it can be used.</param>
        /// <param name="exceptIngredients">The ingredients which can't be used in this recipe (typically exceptions for a category ingredient).</param>
        /// <param name="outputItemIndex">The item ID produced by this recipe, if applicable.</param>
        public RecipeModel(string key, RecipeType type, string displayType, IDictionary<int, int> ingredients, Func<Item, Item> item, bool mustBeLearned, int[] exceptIngredients = null, int? outputItemIndex = null)
        {
            this.Key = key;
            this.Type = type;
            this.DisplayType = displayType;
            this.Ingredients = ingredients;
            this.ExceptIngredients = exceptIngredients ?? new int[0];
            this.Item = item;
            this.MustBeLearned = mustBeLearned;
            this.OutputItemIndex = outputItemIndex;
        }

        /// <summary>Create the item crafted by this recipe.</summary>
        /// <param name="ingredient">The ingredient for which to create an item.</param>
        public Item CreateItem(Item ingredient)
        {
            return this.Item(ingredient);
        }

        /// <summary>Get whether a player knows this recipe.</summary>
        /// <param name="farmer">The farmer to check.</param>
        public bool KnowsRecipe(Farmer farmer)
        {
            return this.Key != null && farmer.knowsRecipe(this.Key);
        }

        /// <summary>Get the number of times this player has crafted the recipe.</summary>
        /// <returns>Returns the times crafted, or -1 if unknown (e.g. some recipe types like furnace aren't tracked).</returns>
        public int GetTimesCrafted(Farmer player)
        {
            switch (this.Type)
            {
                case RecipeType.Cooking:
                    return this.OutputItemIndex.HasValue && player.recipesCooked.TryGetValue(this.OutputItemIndex.Value, out int timesCooked) ? timesCooked : 0;

                case RecipeType.Crafting:
                    return player.craftingRecipes.TryGetValue(this.Key, out int timesCrafted) ? timesCrafted : 0;

                default:
                    return -1;
            }
        }
    }
}
