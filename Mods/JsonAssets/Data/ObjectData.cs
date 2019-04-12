using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StardewValley;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace JsonAssets.Data
{
    public class ObjectData : DataNeedsId
    {
        [JsonIgnore]
        internal Texture2D texture;
        [JsonIgnore]
        internal Texture2D textureColor;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Category_
        {
            // SDV Patcher made these static readonly, so I can't use them in the enum
            Vegetable = -75, //SObject.VegetableCategory,
            Fruit = -79, //SObject.FruitsCategory,
            Flower = -80, //SObject.flowersCategory,
            Gem = -2, //SObject.GemCategory,
            Fish = -4, //SObject.FishCategory,
            Egg = -5, //SObject.EggCategory,
            Milk = -6, //SObject.MilkCategory,
            Cooking = -7, //SObject.CookingCategory,
            Crafting = -8, //SObject.CraftingCategory,
            Mineral = -12, //SObject.mineralsCategory,
            Meat = -14, //SObject.meatCategory,
            Metal = -15, //SObject.metalResources,
            Junk = -20, //SObject.junkCategory,
            Syrup = -27, //SObject.syrupCategory,
            MonsterLoot = -28, //SObject.monsterLootCategory,
            ArtisanGoods = -26, //SObject.artisanGoodsCategory,
            Seeds = -74, //SObject.SeedsCategory,
            Ring = -96, //SObject.ringCategory,
            AnimalGoods = -18, //SObject.sellAtPierresAndMarnies
        }

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

            internal string GetRecipeString( ObjectData parent )
            {
                string str = "";
                foreach (Ingredient ingredient in this.Ingredients)
                    str += Mod.instance.ResolveObjectId(ingredient.Object) + " " + ingredient.Count + " ";
                str = str.Substring(0, str.Length - 1);
                str += $"/what is this for?/{parent.id}/";
                if (parent.Category != Category_.Cooking)
                    str += "false/";
                str += "/null"; // TODO: Requirement
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

        public class FoodBuffs_
        {
            public int Farming { get; set; } = 0;
            public int Fishing { get; set; } = 0;
            public int Mining { get; set; } = 0;
            public int Luck { get; set; } = 0;
            public int Foraging { get; set; } = 0;
            public int MaxStamina { get; set; } = 0;
            public int MagnetRadius { get; set; } = 0;
            public int Speed { get; set; } = 0;
            public int Defense { get; set; } = 0;
            public int Attack { get; set; } = 0;
            public int Duration { get; set; } = 0;
        }
        
        public string Description { get; set; }
        public Category_ Category { get; set; }
        public bool IsColored { get; set; } = false;

        public int Price { get; set; }

        public Recipe_ Recipe { get; set; }

        public int Edibility { get; set; } = SObject.inedible;
        public bool EdibleIsDrink { get; set; } = false;
        public FoodBuffs_ EdibleBuffs = new FoodBuffs_();

        public bool CanPurchase { get; set; } = false;
        public int PurchasePrice { get; set; }
        public string PurchaseFrom { get; set; } = "Pierre";
        public IList<string> PurchaseRequirements { get; set; } = new List<string>();

        public class GiftTastes_
        {
            public IList<string> Love = new List<string>();
            public IList<string> Like = new List<string>();
            public IList<string> Neutral = new List<string>();
            public IList<string> Dislike = new List<string>();
            public IList<string> Hate = new List<string>();
        }
        public GiftTastes_ GiftTastes;

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

        public int GetObjectId() { return this.id; }

        internal string GetObjectInformation()
        {
            if (this.Edibility != SObject.inedible)
            {
                int itype = (int)this.Category;
                string str = $"{this.Name}/{this.Price}/{this.Edibility}/{this.Category} {itype}/{this.LocalizedName()}/{this.LocalizedDescription()}/";
                str += (this.EdibleIsDrink ? "drink" : "food") + "/";
                if (this.EdibleBuffs == null)
                    this.EdibleBuffs = new FoodBuffs_();
                str += $"{this.EdibleBuffs.Farming} {this.EdibleBuffs.Fishing} {this.EdibleBuffs.Mining} 0 {this.EdibleBuffs.Luck} {this.EdibleBuffs.Foraging} 0 {this.EdibleBuffs.MaxStamina} {this.EdibleBuffs.MagnetRadius} {this.EdibleBuffs.Speed} {this.EdibleBuffs.Defense} {this.EdibleBuffs.Attack}/{this.EdibleBuffs.Duration}";
                return str;
            }
            else
            {
                int itype = (int)this.Category;
                return $"{this.Name}/{this.Price}/{this.Edibility}/Basic {itype}/{this.LocalizedName()}/{this.LocalizedDescription()}";
            }
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
