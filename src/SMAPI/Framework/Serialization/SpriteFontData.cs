using System.Collections.Generic;

namespace StardewModdingAPI.Framework.Serialization
{
    internal class SpriteFontData
    {
        public int LineSpacing { get; set; }
        public float Spacing { get; set; }
        public char? DefaultCharacter { get; set; }
        public List<char> Characters { get; set; }
        public Dictionary<char, SpriteFontGlyphData> Glyphs { get; set; }
    }
}
