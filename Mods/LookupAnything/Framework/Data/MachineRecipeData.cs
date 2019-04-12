using System.Collections.Generic;

namespace Pathoschild.Stardew.LookupAnything.Framework.Data
{
    /// <summary>Metadata for a machine recipe.</summary>
    internal class MachineRecipeData
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The machine item ID.</summary>
        public int MachineID { get; set; }

        /// <summary>The items needed to craft the recipe (item ID => number needed).</summary>
        public IDictionary<int, int> Ingredients { get; set; }

        /// <summary>The ingredients which can't be used in this recipe (typically exceptions for a category ingredient).</summary>
        public int[] ExceptIngredients { get; set; }

        /// <summary>The item created by the recipe.</summary>
        public int Output { get; set; }
    }
}
