using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace JsonAssets.Data
{
    public class FruitTreeData : DataNeedsId
    {
        [JsonIgnore]
        internal Texture2D texture;
        
        public object Product { get; set; }
        public string SaplingName { get; set; }
        public string SaplingDescription { get; set; }

        public string Season { get; set; }

        public IList<string> SaplingPurchaseRequirements { get; set; } = new List<string>();
        public int SaplingPurchasePrice { get; set; }
        public string SaplingPurchaseFrom { get; set; } = "Pierre";

        public Dictionary<string, string> SaplingNameLocalization = new Dictionary<string, string>();
        public Dictionary<string, string> SaplingDescriptionLocalization = new Dictionary<string, string>();

        internal ObjectData sapling;
        public int GetSaplingId() { return this.sapling.id; }
        public int GetFruitTreeIndex() { return this.id; }
        internal string GetFruitTreeInformation()
        {
            return $"{this.GetFruitTreeIndex()}/{this.Season}/{Mod.instance.ResolveObjectId(this.Product)}/what goes here?";
        }
    }
}
