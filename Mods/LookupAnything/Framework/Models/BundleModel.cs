using System.Collections.Generic;
using System.Linq;

namespace Pathoschild.Stardew.LookupAnything.Framework.Models
{
    /// <summary>A bundle entry parsed from the game's data files.</summary>
    internal class BundleModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique bundle ID.</summary>
        public int ID { get; }

        /// <summary>The bundle name.</summary>
        public string Name { get; }

        /// <summary>The translated bundle name.</summary>
        public string DisplayName { get; }

        /// <summary>The community center area containing the bundle.</summary>
        public string Area { get; }

        /// <summary>The unparsed reward description, which can be parsed with <see cref="StardewValley.Utility.getItemFromStandardTextDescription"/>.</summary>
        public string RewardData { get; }

        /// <summary>The required item ingredients.</summary>
        public BundleIngredientModel[] Ingredients { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="id">The unique bundle ID.</param>
        /// <param name="name">The bundle name.</param>
        /// <param name="displayName">The translated bundle name.</param>
        /// <param name="area">The community center area containing the bundle.</param>
        /// <param name="rewardData">The unparsed reward description.</param>
        /// <param name="ingredients">The required item ingredients.</param>
        public BundleModel(int id, string name, string displayName, string area, string rewardData, IEnumerable<BundleIngredientModel> ingredients)
        {
            this.ID = id;
            this.Name = name;
            this.DisplayName = displayName;
            this.Area = area;
            this.RewardData = rewardData;
            this.Ingredients = ingredients.ToArray();
        }
    }
}
