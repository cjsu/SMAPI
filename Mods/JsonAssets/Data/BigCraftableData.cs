using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;
using System.Collections.Generic;

namespace JsonAssets.Data
{
    public class BigCraftableData : DataNeedsId
    {
        [JsonIgnore]
        internal Texture2D texture;

        public class Recipe_
        {
            public class Ingredient
            {
                public object Object { get; set; }
                public int Count { get; set; }
            }
            // Possibly friendship option (letters, like vanilla) and/or skill levels (on levelup?)
            public int ResultCount { get; set; } = 1;
            public IList<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

            public bool IsDefault { get; set; } = false;
            public bool CanPurchase { get; set; } = false;
            public int PurchasePrice { get; set; }
            public string PurchaseFrom { get; set; } = "Gus";
            public IList<string> PurchaseRequirements { get; set; } = new List<string>();

            internal string GetRecipeString( BigCraftableData parent )
            {
                string str = "";
                foreach (Ingredient ingredient in this.Ingredients)
                    str += Mod.instance.ResolveObjectId(ingredient.Object) + " " + ingredient.Count + " ";
                str = str.Substring(0, str.Length - 1);
                str += $"/what is this for?/{parent.id}/true/null";
                return str;
            }

            internal string GetPurchaseRequirementString()
            {
                string str = $"1234567890";
                foreach (string cond in this.PurchaseRequirements)
                    str += $"/{cond}";
                return str;
            }
        }
        
        public string Description { get; set; }

        public int Price { get; set; }

        public bool ProvidesLight { get; set; } = false;

        public Recipe_ Recipe { get; set; }

        public bool CanPurchase { get; set; } = false;
        public int PurchasePrice { get; set; }
        public string PurchaseFrom { get; set; } = "Pierre";
        public IList<string> PurchaseRequirements { get; set; } = new List<string>();

        public Dictionary<string, string> NameLocalization = new Dictionary<string, string>();
        public Dictionary<string, string> DescriptionLocalization = new Dictionary<string, string>();

        public string LocalizedName()
        {
            LocalizedContentManager.LanguageCode currLang = LocalizedContentManager.CurrentLanguageCode;
            if (currLang == LocalizedContentManager.LanguageCode.en)
                return this.Name;
            if (this.NameLocalization == null || !this.NameLocalization.ContainsKey(currLang.ToString()))
                return this.Name;
            return this.NameLocalization[currLang.ToString()];
        }

        public string LocalizedDescription()
        {
            LocalizedContentManager.LanguageCode currLang = LocalizedContentManager.CurrentLanguageCode;
            if (currLang == LocalizedContentManager.LanguageCode.en)
                return this.Description;
            if (this.DescriptionLocalization == null || !this.DescriptionLocalization.ContainsKey(currLang.ToString()))
                return this.Description;
            return this.DescriptionLocalization[currLang.ToString()];
        }

        public int GetCraftableId() { return this.id; }

        internal string GetCraftableInformation()
        {
            string str = $"{this.Name}/{this.Price}/-300/Crafting -9/{this.LocalizedDescription()}/true/true/0/{this.LocalizedName()}";
            if (this.ProvidesLight)
                str += "/true";
            return str;
        }

        internal string GetPurchaseRequirementString()
        {
            string str = $"1234567890";
            foreach (string cond in this.PurchaseRequirements)
                str += $"/{cond}";
            return str;
        }
    }
}
