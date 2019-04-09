using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ConvenientChests.CategorizeChests.Interface
{
    static class Sprites
    {
        public static readonly NineSlice TabBackground = new NineSlice
        {
            TopLeft     = new TextureRegion(Game1.mouseCursors, new Rectangle(0, 384, 5, 5), zoom: true),
            TopRight    = new TextureRegion(Game1.mouseCursors, new Rectangle(11, 384, 5, 5), zoom: true),
            BottomLeft  = new TextureRegion(Game1.mouseCursors, new Rectangle(0, 395, 5, 5), zoom: true),
            BottomRight = new TextureRegion(Game1.mouseCursors, new Rectangle(11, 395, 5, 5), zoom: true),
            Top         = new TextureRegion(Game1.mouseCursors, new Rectangle(4, 384, 1, 3), zoom: true),
            Left        = new TextureRegion(Game1.mouseCursors, new Rectangle(0, 388, 3, 1), zoom: true),
            Right       = new TextureRegion(Game1.mouseCursors, new Rectangle(13, 388, 3, 1), zoom: true),
            Bottom      = new TextureRegion(Game1.mouseCursors, new Rectangle(4, 397, 1, 3), zoom: true),
            Center      = new TextureRegion(Game1.mouseCursors, new Rectangle(5, 387, 1, 1), zoom: true),
        };

        public static readonly NineSlice MenuBackground = new NineSlice
        {
            TopLeft     = new TextureRegion(Game1.menuTexture, new Rectangle(12, 12, 24, 24)),
            TopRight    = new TextureRegion(Game1.menuTexture, new Rectangle(220, 12, 24, 24)),
            BottomLeft  = new TextureRegion(Game1.menuTexture, new Rectangle(12, 220, 24, 24)),
            BottomRight = new TextureRegion(Game1.menuTexture, new Rectangle(220, 220, 24, 24)),
            Top         = new TextureRegion(Game1.menuTexture, new Rectangle(40, 12, 1, 24)),
            Left        = new TextureRegion(Game1.menuTexture, new Rectangle(12, 36, 24, 1)),
            Right       = new TextureRegion(Game1.menuTexture, new Rectangle(220, 40, 24, 1)),
            Bottom      = new TextureRegion(Game1.menuTexture, new Rectangle(36, 220, 1, 24)),
            Center      = new TextureRegion(Game1.menuTexture, new Rectangle(64, 128, 64, 64)),
        };

        public static readonly NineSlice TooltipBackground = new NineSlice
        {
            TopLeft     = new TextureRegion(Game1.mouseCursors, new Rectangle(293, 360, 4, 4), zoom: true),
            Left        = new TextureRegion(Game1.mouseCursors, new Rectangle(293, 364, 4, 16), zoom: true),
            BottomLeft  = new TextureRegion(Game1.mouseCursors, new Rectangle(293, 380, 4, 4), zoom: true),
            Bottom      = new TextureRegion(Game1.mouseCursors, new Rectangle(297, 380, 16, 4), zoom: true),
            BottomRight = new TextureRegion(Game1.mouseCursors, new Rectangle(313, 380, 4, 4), zoom: true),
            Right       = new TextureRegion(Game1.mouseCursors, new Rectangle(313, 364, 4, 16), zoom: true),
            TopRight    = new TextureRegion(Game1.mouseCursors, new Rectangle(313, 360, 4, 4), zoom: true),
            Top         = new TextureRegion(Game1.mouseCursors, new Rectangle(297, 360, 16, 4), zoom: true),
            Center      = new TextureRegion(Game1.mouseCursors, new Rectangle(297, 364, 16, 16), zoom: true),
        };

        public static readonly NineSlice LeftProtrudingTab = new NineSlice
        {
            TopLeft     = new TextureRegion(Game1.mouseCursors, new Rectangle(656, 64, 5, 5), zoom: true),
            TopRight    = new TextureRegion(Game1.mouseCursors, new Rectangle(670, 64, 2, 5), zoom: true),
            BottomLeft  = new TextureRegion(Game1.mouseCursors, new Rectangle(656, 75, 5, 5), zoom: true),
            BottomRight = new TextureRegion(Game1.mouseCursors, new Rectangle(670, 75, 2, 5), zoom: true),
            Top         = new TextureRegion(Game1.mouseCursors, new Rectangle(661, 64, 1, 4), zoom: true),
            Left        = new TextureRegion(Game1.mouseCursors, new Rectangle(656, 69, 5, 1), zoom: true),
            Right       = new TextureRegion(Game1.mouseCursors, new Rectangle(670, 68, 2, 1), zoom: true),
            Bottom      = new TextureRegion(Game1.mouseCursors, new Rectangle(661, 76, 1, 4), zoom: true),
            Center      = new TextureRegion(Game1.mouseCursors, new Rectangle(661, 68, 1, 1), zoom: true),
        };

        public static readonly TextureRegion LeftArrow      = new TextureRegion(Game1.mouseCursors, new Rectangle(8, 268, 44, 40));
        public static readonly TextureRegion RightArrow     = new TextureRegion(Game1.mouseCursors, new Rectangle(12, 204, 44, 40));
        public static readonly TextureRegion EmptyCheckbox  = new TextureRegion(Game1.mouseCursors, new Rectangle(227, 425, 9, 9), zoom: true);
        public static readonly TextureRegion FilledCheckbox = new TextureRegion(Game1.mouseCursors, new Rectangle(236, 425, 9, 9), zoom: true);
        public static readonly TextureRegion ExitButton     = new TextureRegion(Game1.mouseCursors, new Rectangle(337, 494, 12, 12), zoom: true);

        public static void Draw(this SpriteBatch batch, Texture2D sheet, Rectangle sprite, int x, int y, int width, int height, Color? color = null)
        {
            batch.Draw(sheet, new Rectangle(x, y, width, height), sprite, color ?? Color.White);
        }

        public static void Draw(this SpriteBatch batch, TextureRegion textureRegion, int x, int y, int width, int height, Color? color = null)
        {
            batch.Draw(textureRegion.Texture, textureRegion.Region, x, y, width, height, color);
        }
    }
}