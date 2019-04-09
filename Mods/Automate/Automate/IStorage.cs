using System;
using System.Collections.Generic;
using Pathoschild.Stardew.Automate.Framework;

namespace Pathoschild.Stardew.Automate
{
    /// <summary>Manages access to items in the underlying containers.</summary>
    public interface IStorage
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get all items from the given pipes.</summary>
        IEnumerable<ITrackedStack> GetItems();

        /****
        ** TryGetIngredient
        ****/
        /// <summary>Get an ingredient needed for a recipe.</summary>
        /// <param name="predicate">Returns whether an item should be matched.</param>
        /// <param name="count">The number of items to find.</param>
        /// <param name="consumable">The matching consumables.</param>
        /// <returns>Returns whether the requirement is met.</returns>
        bool TryGetIngredient(Func<ITrackedStack, bool> predicate, int count, out IConsumable consumable);

        /// <summary>Get an ingredient needed for a recipe.</summary>
        /// <param name="id">The item or category ID.</param>
        /// <param name="count">The number of items to find.</param>
        /// <param name="consumable">The matching consumables.</param>
        /// <returns>Returns whether the requirement is met.</returns>
        bool TryGetIngredient(int id, int count, out IConsumable consumable);

        /// <summary>Get an ingredient needed for a recipe.</summary>
        /// <param name="recipes">The items to match.</param>
        /// <param name="consumable">The matching consumables.</param>
        /// <param name="recipe">The matched requisition.</param>
        /// <returns>Returns whether the requirement is met.</returns>
        bool TryGetIngredient(IRecipe[] recipes, out IConsumable consumable, out IRecipe recipe);

        /****
        ** TryConsume
        ****/
        /// <summary>Consume an ingredient needed for a recipe.</summary>
        /// <param name="predicate">Returns whether an item should be matched.</param>
        /// <param name="count">The number of items to find.</param>
        /// <returns>Returns whether the item was consumed.</returns>
        bool TryConsume(Func<ITrackedStack, bool> predicate, int count);

        /// <summary>Consume an ingredient needed for a recipe.</summary>
        /// <param name="itemID">The item ID.</param>
        /// <param name="count">The number of items to find.</param>
        /// <returns>Returns whether the item was consumed.</returns>
        bool TryConsume(int itemID, int count);

        /****
        ** TryPush
        ****/
        /// <summary>Add the given item stack to the pipes if there's space.</summary>
        /// <param name="item">The item stack to push.</param>
        bool TryPush(ITrackedStack item);
    }
}
