using System.Collections.Generic;

namespace JsonAssets.Data
{
    public class ContentPackData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public IList<string> UpdateKeys { get; set; } = new List<string>();
    }
}
