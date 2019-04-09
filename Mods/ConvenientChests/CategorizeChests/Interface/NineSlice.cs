using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ConvenientChests.CategorizeChests.Interface
{
    class NineSlice
    {
        public TextureRegion Center;
        public TextureRegion Top;
        public TextureRegion TopRight;
        public TextureRegion Right;
        public TextureRegion BottomRight;
        public TextureRegion Bottom;
        public TextureRegion BottomLeft;
        public TextureRegion Left;
        public TextureRegion TopLeft;

        public int RightBorderThickness => Right.Width;
        public int LeftBorderThickness => Left.Width;
        public int TopBorderThickness => Top.Height;
        public int BottomBorderThickness => Bottom.Height;

        public void Draw(SpriteBatch batch, Rectangle bounds)
        {
            // draw background
            batch.Draw(Center,
                bounds.X + Left.Width,
                bounds.Y + Top.Height,
                bounds.Width - Left.Width - Right.Width,
                bounds.Height - Top.Height - Bottom.Height);

            // draw borders
            batch.Draw(Top,
                bounds.X + TopLeft.Width,
                bounds.Y,
                bounds.Width - TopLeft.Width - TopRight.Width,
                Top.Height);
            batch.Draw(Left,
                bounds.X,
                bounds.Y + TopLeft.Height,
                Left.Width,
                bounds.Height - TopLeft.Height - BottomLeft.Height);
            batch.Draw(Right,
                bounds.X + bounds.Width - Right.Width,
                bounds.Y + TopRight.Height,
                Right.Width,
                bounds.Height - TopRight.Height - BottomRight.Height);
            batch.Draw(Bottom,
                bounds.X + BottomLeft.Width,
                bounds.Y + bounds.Height - Bottom.Height,
                bounds.Width - BottomLeft.Width - BottomRight.Width,
                Bottom.Height);

            // draw border joints
            batch.Draw(TopLeft,
                bounds.X,
                bounds.Y,
                TopLeft.Width,
                TopLeft.Height);
            batch.Draw(TopRight,
                bounds.X + bounds.Width - TopRight.Width,
                bounds.Y,
                TopRight.Width,
                TopRight.Height);
            batch.Draw(BottomLeft,
                bounds.X,
                bounds.Y + bounds.Height - BottomLeft.Height,
                BottomLeft.Width,
                BottomLeft.Height);
            batch.Draw(BottomRight,
                bounds.X + bounds.Width - BottomRight.Width,
                bounds.Y + bounds.Height - BottomRight.Height,
                BottomRight.Width,
                BottomRight.Height);
        }
    }
}