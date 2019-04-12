using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Components;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field which shows a progress bar UI.</summary>
    internal class PercentageBarField : GenericField
    {
        /*********
        ** Fields
        *********/
        /// <summary>The current progress value.</summary>
        protected readonly int CurrentValue;

        /// <summary>The maximum progress value.</summary>
        protected readonly int MaxValue;

        /// <summary>The text to show next to the progress bar (if any).</summary>
        protected readonly string Text;

        /// <summary>The color of the filled bar.</summary>
        protected readonly Color FilledColor;

        /// <summary>The color of the empty bar.</summary>
        protected readonly Color EmptyColor;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="currentValue">The current progress value.</param>
        /// <param name="maxValue">The maximum progress value.</param>
        /// <param name="filledColor">The color of the filled bar.</param>
        /// <param name="emptyColor">The color of the empty bar.</param>
        /// <param name="text">The text to show next to the progress bar (if any).</param>
        public PercentageBarField(GameHelper gameHelper, string label, int currentValue, int maxValue, Color filledColor, Color emptyColor, string text)
            : base(gameHelper, label, hasValue: true)
        {
            this.CurrentValue = currentValue;
            this.MaxValue = maxValue;
            this.FilledColor = filledColor;
            this.EmptyColor = emptyColor;
            this.Text = text;
        }

        /// <summary>Draw the value (or return <c>null</c> to render the <see cref="GenericField.Value"/> using the default format).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="font">The recommended font.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="wrapWidth">The maximum width before which content should be wrapped.</param>
        /// <returns>Returns the drawn dimensions, or <c>null</c> to draw the <see cref="GenericField.Value"/> using the default format.</returns>
        public override Vector2? DrawValue(SpriteBatch spriteBatch, SpriteFont font, Vector2 position, float wrapWidth)
        {
            Vector2 barSize = this.DrawBar(spriteBatch, position, this.CurrentValue / (this.MaxValue * 1f), this.FilledColor, this.EmptyColor, wrapWidth);
            Vector2 textSize = !string.IsNullOrWhiteSpace(this.Text)
                ? spriteBatch.DrawTextBlock(font, this.Text, new Vector2(position.X + barSize.X + 3, position.Y), wrapWidth)
                : Vector2.Zero;
            return new Vector2(barSize.X + 3 + textSize.X, Math.Max(barSize.Y, textSize.Y));
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Draw a percentage bar.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="ratio">The percentage value (between 0 and 1).</param>
        /// <param name="filledColor">The color of the filled bar.</param>
        /// <param name="emptyColor">The color of the empty bar.</param>
        /// <param name="maxWidth">The maximum width before which content should be wrapped.</param>
        /// <returns>Returns the drawn dimensions, or <c>null</c> to draw the <see cref="GenericField.Value"/> using the default format.</returns>
        protected Vector2 DrawBar(SpriteBatch spriteBatch, Vector2 position, float ratio, Color filledColor, Color emptyColor, float maxWidth = 100)
        {
            int barHeight = 22;
            ratio = Math.Min(1f, ratio);
            float width = Math.Min(100, maxWidth);
            float filledWidth = width * ratio;
            float emptyWidth = width - filledWidth;

            if (filledWidth > 0)
                spriteBatch.Draw(Sprites.Pixel, new Rectangle((int)position.X, (int)position.Y, (int)filledWidth, barHeight), filledColor);
            if (emptyWidth > 0)
                spriteBatch.Draw(Sprites.Pixel, new Rectangle((int)(position.X + filledWidth), (int)position.Y, (int)emptyWidth, barHeight), emptyColor);

            return new Vector2(width, barHeight);
        }
    }
}
