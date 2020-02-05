namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class ModConfig
    {
        public Toggle vToggle = new Toggle(new Rect(36, 12, 64, 64));
        public VirtualButton[] buttons { get; set;} = new VirtualButton[] {
            new VirtualButton(SButton.Q, new Rect(192, 80, 90, 90), 0.5f),
            new VirtualButton(SButton.I, new Rect(288, 80, 90, 90), 0.5f),
            new VirtualButton(SButton.O, new Rect(384, 80, 90, 90), 0.5f),
            new VirtualButton(SButton.P, new Rect(480, 80, 90, 90), 0.5f)
        };
        public VirtualButton[] buttonsExtend { get; set; } = new VirtualButton[] {
            new VirtualButton(SButton.MouseRight, new Rect(192, 170, 162, 90), 0.5f, "RightMouse"),
            new VirtualButton(SButton.RightWindows, new Rect(362, 170, 162, 90), 0.5f, "Command"),
            new VirtualButton(SButton.RightControl, new Rect(532, 170, 162, 90), 0.5f, "Console")
        };
        internal class VirtualButton {
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
        internal class Toggle
        {
            public Rect rectangle;
            //public float scale;

            public Toggle(Rect rectangle)
            {
                this.rectangle = rectangle;
                //this.scale = scale;
            }
        }
        internal class Rect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;

            public Rect(int x, int y, int width, int height)
            {
                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
            }
        }
    }
}
