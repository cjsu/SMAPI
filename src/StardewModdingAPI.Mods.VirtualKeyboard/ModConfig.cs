using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class ModConfig
    {
        public VirtualButton[] buttons { get; set; } = new VirtualButton[] {
            new VirtualButton(SButton.Q, new Rect(192, 150, 90, 90, 6), 0.5f),
            new VirtualButton(SButton.I, new Rect(288, 150, 90, 90, 6), 0.5f),
            new VirtualButton(SButton.O, new Rect(384, 150, 90, 90, 6), 0.5f),
            new VirtualButton(SButton.P, new Rect(480, 150, 90, 90, 6), 0.5f)
        };
        internal class VirtualButton
        {
            public SButton key;
            public Rect rectangle;
            public float transparency;
            public string alias;
            public VirtualButton(SButton key, Rect rectangle, float transparency, string alias = null)
            {
                this.key = key;
                this.rectangle = rectangle;
                this.transparency = transparency;
                this.alias = alias;
            }
        }
        internal class Rect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int Padding;

            public Rect(int x, int y, int width, int height, int padding)
            {
                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
                this.Padding = padding;
            }
        }
    }
}
