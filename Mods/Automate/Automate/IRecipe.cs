using System;
using StardewValley;
using Object = StardewValley.Object;

namespace Pathoschild.Stardew.Automate
{
    /// <summary>Describes a generic recipe based on item input and output.</summary>
    public interface IRecipe
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The input item or category ID.</summary>
        int InputID { get; }

        /// <summary>The number of inputs needed.</summary>
        int InputCount { get; }

        /// <summary>The output to generate (given an input).</summary>
        Func<Item, Object> Output { get; }

        /// <summary>The time needed to prepare an output.</summary>
        int Minutes { get; }


        /*********
        ** Methods
        *********/
        /// <summary>Get whether the recipe can accept a given item as input (regardless of stack size).</summary>
        /// <param name="stack">The item to check.</param>
        bool AcceptsInput(ITrackedStack stack);
    }
}
