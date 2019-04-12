using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StardewValley;
using StardewValley.Tools;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace JsonAssets.Data
{
    public class WeaponData : DataNeedsId
    {
        [JsonIgnore]
        internal Texture2D texture;

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Type_
        {
            Dagger = MeleeWeapon.dagger,
            Club = MeleeWeapon.club,
            Sword = MeleeWeapon.defenseSword,
        }
        
        public string Description { get; set; }
        public Type_ Type { get; set; }

        public int MinimumDamage { get; set; }
        public int MaximumDamage { get; set; }
        public double Knockback { get; set; }
        public int Speed { get; set; }
        public int Accuracy { get; set; }
        public int Defense { get; set; }
        public int MineDropVar { get; set; }
        public int MineDropMinimumLevel { get; set; }
        public int ExtraSwingArea { get; set; }
        public double CritChance { get; set; }
        public double CritMultiplier { get; set; }

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

        public int GetWeaponId() { return this.id; }

        internal string GetWeaponInformation()
        {
            return $"{this.Name}/{this.LocalizedDescription()}/{this.MinimumDamage}/{this.MaximumDamage}/{this.Knockback}/{this.Speed}/{this.Accuracy}/{this.Defense}/{(int)this.Type}/{this.MineDropVar}/{this.MineDropMinimumLevel}/{this.ExtraSwingArea}/{this.CritChance}/{this.CritMultiplier}/{this.LocalizedName()}";
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
