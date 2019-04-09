using System;
using System.Collections.Generic;
using System.Linq;
using Pathoschild.Stardew.Automate.Framework.Storage;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework
{
    /// <summary>Manages access to items in the underlying containers.</summary>
    internal class StorageManager : IStorage
    {
        /*********
        ** Fields
        *********/
        /// <summary>The storage containers.</summary>
        private readonly IContainer[] Containers;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="containers">The storage containers.</param>
        public StorageManager(IEnumerable<IContainer> containers)
        {
            this.Containers = containers.ToArray();
        }

        /****
        ** GetItems
        ****/
        /// <summary>Get all items from the given pipes.</summary>
        public IEnumerable<ITrackedStack> GetItems()
        {
            foreach (IContainer container in this.Containers)
            {
                if (!container.AllowsOutput())
                    continue;

                foreach (ITrackedStack item in container)
                    yield return item;
            }
        }

        /****
        ** TryGetIngredient
        ****/
        /// <summary>Get an ingredient needed for a recipe.</summary>
        /// <param name="predicate">Returns whether an item should be matched.</param>
        /// <param name="count">The number of items to find.</param>
        /// <param name="consumable">The matching consumables.</param>
        /// <returns>Returns whether the requirement is met.</returns>
        public bool TryGetIngredient(Func<ITrackedStack, bool> predicate, int count, out IConsumable consumable)
        {
            int countMissing = count;
            ITrackedStack[] consumables = this.GetItems().Where(predicate)
                .TakeWhile(chestItem =>
                {
                    if (countMissing <= 0)
                        return false;

                    countMissing -= chestItem.Count;
                    return true;
                })
                .ToArray();

            consumable = new Consumable(new TrackedItemCollection(consumables), count);
            return consumable.IsMet;
        }

        /// <summary>Get an ingredient needed for a recipe.</summary>
        /// <param name="id">The item or category ID.</param>
        /// <param name="count">The number of items to find.</param>
        /// <param name="consumable">The matching consumables.</param>
        /// <returns>Returns whether the requirement is met.</returns>
        public bool TryGetIngredient(int id, int count, out IConsumable consumable)
        {
            return this.TryGetIngredient(item => item.Sample.ParentSheetIndex == id || item.Sample.Category == id, count, out consumable);
        }

        /// <summary>Get an ingredient needed for a recipe.</summary>
        /// <param name="recipes">The items to match.</param>
        /// <param name="consumable">The matching consumables.</param>
        /// <param name="recipe">The matched requisition.</param>
        /// <returns>Returns whether the requirement is met.</returns>
        public bool TryGetIngredient(IRecipe[] recipes, out IConsumable consumable, out IRecipe recipe)
        {
            IDictionary<IRecipe, List<ITrackedStack>> accumulator = recipes.ToDictionary(req => req, req => new List<ITrackedStack>());

            foreach (ITrackedStack stack in this.GetItems())
            {
                foreach (var entry in accumulator)
                {
                    recipe = entry.Key;
                    List<ITrackedStack> found = entry.Value;

                    if (recipe.AcceptsInput(stack))
                    {
                        found.Add(stack);
                        if (found.Sum(p => p.Count) >= recipe.InputCount)
                        {
                            consumable = new Consumable(new TrackedItemCollection(found), entry.Key.InputCount);
                            return true;
                        }
                    }
                }
            }

            consumable = null;
            recipe = null;
            return false;
        }

        /****
        ** TryConsume
        ****/
        /// <summary>Consume an ingredient needed for a recipe.</summary>
        /// <param name="predicate">Returns whether an item should be matched.</param>
        /// <param name="count">The number of items to find.</param>
        /// <returns>Returns whether the item was consumed.</returns>
        public bool TryConsume(Func<ITrackedStack, bool> predicate, int count)
        {
            if (this.TryGetIngredient(predicate, count, out IConsumable requirement))
            {
                requirement.Reduce();
                return true;
            }
            return false;
        }

        /// <summary>Consume an ingredient needed for a recipe.</summary>
        /// <param name="itemID">The item ID.</param>
        /// <param name="count">The number of items to find.</param>
        /// <returns>Returns whether the item was consumed.</returns>
        public bool TryConsume(int itemID, int count)
        {
            return this.TryConsume(item => item.Sample.ParentSheetIndex == itemID, count);
        }

        /****
        ** TryPush
        ****/
        /// <summary>Add the given item stack to the pipes if there's space.</summary>
        /// <param name="item">The item stack to push.</param>
        public bool TryPush(ITrackedStack item)
        {
            if (item == null || item.Count <= 0)
                return false;

            int originalCount = item.Count;

            var preferOutputContainers = this.Containers.Where(p => p.AllowsInput() && p.PreferForOutput());
            var otherContainers = this.Containers.Where(p => p.AllowsInput() && !p.PreferForOutput());

            // push into 'output' chests
            foreach (IContainer container in preferOutputContainers)
            {
                container.Store(item);
                if (item.Count <= 0)
                    return true;
            }

            // push into chests that already have this item
            string itemKey = this.GetItemKey(item.Sample);
            foreach (IContainer container in otherContainers)
            {
                if (container.All(p => this.GetItemKey(p.Sample) != itemKey))
                    continue;

                container.Store(item);
                if (item.Count <= 0)
                    return true;
            }

            // push into first available chest
            if (item.Count >= 0)
            {
                foreach (IContainer container in otherContainers)
                {
                    container.Store(item);
                    if (item.Count <= 0)
                        return true;
                }
            }

            return item.Count < originalCount;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a key which uniquely identifies an item type.</summary>
        /// <param name="item">The item to identify.</param>
        private string GetItemKey(Item item)
        {
            string key = item.GetType().FullName;
            if (item is SObject obj)
                key += "_craftable:" + obj.bigCraftable.Value;
            key += "_id:" + item.ParentSheetIndex;

            return key;
        }
    }
}
