using System;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework
{
    /// <summary>Describes a generic recipe based on item input and output.</summary>
    internal class Recipe : IRecipe
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The input item or category ID.</summary>
        public int InputID { get; }

        /// <summary>The number of inputs needed.</summary>
        public int InputCount { get; }

        /// <summary>The output to generate (given an input).</summary>
        public Func<Item, SObject> Output { get; }

        /// <summary>The time needed to prepare an output.</summary>
        public int Minutes { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="input">The input item or category ID.</param>
        /// <param name="inputCount">The number of inputs needed.</param>
        /// <param name="output">The output to generate (given an input).</param>
        /// <param name="minutes">The time needed to prepare an output.</param>
        public Recipe(int input, int inputCount, Func<Item, SObject> output, int minutes)
        {
            this.InputID = input;
            this.InputCount = inputCount;
            this.Output = output;
            this.Minutes = minutes;
        }

        /// <summary>Get whether the recipe can accept a given item as input (regardless of stack size).</summary>
        /// <param name="stack">The item to check.</param>
        public bool AcceptsInput(ITrackedStack stack)
        {
            return stack.Sample.ParentSheetIndex == this.InputID || stack.Sample.Category == this.InputID;
        }
    }
}
