using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Pathoschild.Stardew.Common.UI
{
    /// <summary>Simplifies access to the game's sprite sheets.</summary>
    /// <remarks>Each sprite is represented by a rectangle, which specifies the coordinates and dimensions of the image in the sprite sheet.</remarks>
    internal static class CommonSprites
    {
        /// <summary>Sprites used to draw a button.</summary>
        public static class Button
        {
            /// <summary>The sprite sheet containing the icon sprites.</summary>
            public static Texture2D Sheet => Game1.mouseCursors;

            /// <summary>The legend background.</summary>
            public static readonly Rectangle Background = new Rectangle(297, 364, 1, 1);

            /// <summary>The top border.</summary>
            public static readonly Rectangle Top = new Rectangle(279, 284, 1, 4);

            /// <summary>The bottom border.</summary>
            public static readonly Rectangle Bottom = new Rectangle(279, 296, 1, 4);

            /// <summary>The left border.</summary>
            public static readonly Rectangle Left = new Rectangle(274, 289, 4, 1);

            /// <summary>The right border.</summary>
            public static readonly Rectangle Right = new Rectangle(286, 289, 4, 1);

            /// <summary>The top-left corner.</summary>
            public static readonly Rectangle TopLeft = new Rectangle(274, 284, 4, 4);

            /// <summary>The top-right corner.</summary>
            public static readonly Rectangle TopRight = new Rectangle(286, 284, 4, 4);

            /// <summary>The bottom-left corner.</summary>
            public static readonly Rectangle BottomLeft = new Rectangle(274, 296, 4, 4);

            /// <summary>The bottom-right corner.</summary>
            public static readonly Rectangle BottomRight = new Rectangle(286, 296, 4, 4);
        }

        /// <summary>Sprites used to draw a scroll.</summary>
        public static class Scroll
        {
            /// <summary>The sprite sheet containing the icon sprites.</summary>
            public static Texture2D Sheet => Game1.mouseCursors;

            /// <summary>The legend background.</summary>
            public static readonly Rectangle Background = new Rectangle(334, 321, 1, 1);

            /// <summary>The top border.</summary>
            public static readonly Rectangle Top = new Rectangle(331, 318, 1, 2);

            /// <summary>The bottom border.</summary>
            public static readonly Rectangle Bottom = new Rectangle(327, 334, 1, 2);

            /// <summary>The left border.</summary>
            public static readonly Rectangle Left = new Rectangle(325, 320, 6, 1);

            /// <summary>The right border.</summary>
            public static readonly Rectangle Right = new Rectangle(344, 320, 6, 1);

            /// <summary>The top-left corner.</summary>
            public static readonly Rectangle TopLeft = new Rectangle(325, 318, 6, 2);

            /// <summary>The top-right corner.</summary>
            public static readonly Rectangle TopRight = new Rectangle(344, 318, 6, 2);

            /// <summary>The bottom-left corner.</summary>
            public static readonly Rectangle BottomLeft = new Rectangle(325, 334, 6, 2);

            /// <summary>The bottom-right corner.</summary>
            public static readonly Rectangle BottomRight = new Rectangle(344, 334, 6, 2);
        }
    }
}
