using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using StardewValley;
using System.Collections.Generic;

namespace JsonAssets.Data
{
    class HatData : DataNeedsId
    {
        [JsonIgnore]
        internal Texture2D texture;

        public string Description { get; set; }
        public int PurchasePrice { get; set; }
        public bool ShowHair { get; set; }
        public bool IgnoreHairstyleOffset { get; set; }

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

        public int GetHatId() { return this.id; }

        internal string GetHatInformation()
        {
            return $"{this.Name}/{this.LocalizedDescription()}/" + (this.ShowHair ? "true" : "false" ) + "/" + (this.IgnoreHairstyleOffset ? "true" : "false") + $"/{this.LocalizedName()}";
        }
    }
}
