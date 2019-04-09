using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConvenientChests.CategorizeChests.Interface.Widgets
{
    /// <summary>
    /// A resizable nine-slice background.
    /// </summary>
    class Background : Widget
    {
        public readonly NineSlice Graphic;

        public Background(NineSlice nineSlice)
        {
            Graphic = nineSlice;
        }

        public Background(NineSlice nineSlice, int width, int height)
        {
            Graphic = nineSlice;
            Width = width;
            Height = height;
        }

        public override void Draw(SpriteBatch batch)
        {
            Graphic.Draw(batch, new Rectangle(GlobalPosition.X, GlobalPosition.Y, Width, Height));
        }
    }
}