using System.Collections.Generic;

namespace Pathoschild.Stardew.LookupAnything.Framework.Data
{
    /// <summary>Metadata for a building recipe.</summary>
    internal class BuildingRecipeData
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The building key.</summary>
        public string BuildingKey { get; set; }

        /// <summary>The items needed to craft the recipe (item ID => number needed).</summary>
        public IDictionary<int, int> Ingredients { get; set; }

        /// <summary>The ingredients which can't be used in this recipe (typically exceptions for a category ingredient).</summary>
        public int[] ExceptIngredients { get; set; }

        /// <summary>The item created by the recipe.</summary>
        public int Output { get; set; }
    }
}
