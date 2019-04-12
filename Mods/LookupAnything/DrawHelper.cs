using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.LookupAnything.Components;
using Pathoschild.Stardew.LookupAnything.Framework;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything
{
    /// <summary>Provides utility methods for drawing to the screen.</summary>
    internal static class DrawHelper
    {
        /*********
        ** Public methods
        *********/
        /****
        ** Fonts
        ****/
        /// <summary>Get the dimensions of a space character.</summary>
        /// <param name="font">The font to measure.</param>
        public static float GetSpaceWidth(SpriteFont font)
        {
            return CommonHelper.GetSpaceWidth(font);
        }

        /****
        ** Drawing
        ****/
        /// <summary>Draw a sprite to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="sheet">The sprite sheet containing the sprite.</param>
        /// <param name="sprite">The sprite coordinates and dimensions in the sprite sheet.</param>
        /// <param name="x">The X-position at which to draw the sprite.</param>
        /// <param name="y">The X-position at which to draw the sprite.</param>
        /// <param name="color">The color to tint the sprite.</param>
        /// <param name="scale">The scale to draw.</param>
        public static void DrawSprite(this SpriteBatch spriteBatch, Texture2D sheet, Rectangle sprite, float x, float y, Color? color = null, float scale = 1)
        {
            spriteBatch.Draw(sheet, new Vector2(x, y), sprite, color ?? Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        /// <summary>Draw a sprite to the screen scaled and centered to fit the given dimensions.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="sprite">The sprite to draw.</param>
        /// <param name="x">The X-position at which to draw the sprite.</param>
        /// <param name="y">The X-position at which to draw the sprite.</param>
        /// <param name="size">The size to draw.</param>
        /// <param name="color">The color to tint the sprite.</param>
        public static void DrawSpriteWithin(this SpriteBatch spriteBatch, SpriteInfo sprite, float x, float y, Vector2 size, Color? color = null)
        {
            spriteBatch.DrawSpriteWithin(sprite.Spritesheet, sprite.SourceRectangle, x, y, size, color ?? Color.White);
        }

        /// <summary>Draw a sprite to the screen scaled and centered to fit the given dimensions.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="sheet">The sprite sheet containing the sprite.</param>
        /// <param name="sprite">The sprite coordinates and dimensions in the sprite sheet.</param>
        /// <param name="x">The X-position at which to draw the sprite.</param>
        /// <param name="y">The X-position at which to draw the sprite.</param>
        /// <param name="size">The size to draw.</param>
        /// <param name="color">The color to tint the sprite.</param>
        public static void DrawSpriteWithin(this SpriteBatch spriteBatch, Texture2D sheet, Rectangle sprite, float x, float y, Vector2 size, Color? color = null)
        {
            // calculate dimensions
            float largestDimension = Math.Max(sprite.Width, sprite.Height);
            float scale = size.X / largestDimension;
            float leftOffset = Math.Max((size.X - (sprite.Width * scale)) / 2, 0);
            float topOffset = Math.Max((size.Y - (sprite.Height * scale)) / 2, 0);

            // draw
            spriteBatch.DrawSprite(sheet, sprite, x + leftOffset, y + topOffset, color ?? Color.White, scale);
        }

        /// <summary>Draw a sprite to the screen.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="x">The X-position at which to start the line.</param>
        /// <param name="y">The X-position at which to start the line.</param>
        /// <param name="size">The line dimensions.</param>
        /// <param name="color">The color to tint the sprite.</param>
        public static void DrawLine(this SpriteBatch batch, float x, float y, Vector2 size, Color? color = null)
        {
            batch.Draw(Sprites.Pixel, new Rectangle((int)x, (int)y, (int)size.X, (int)size.Y), color ?? Color.White);
        }

        /// <summary>Draw a block of text to the screen with the specified wrap width.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="font">The sprite font.</param>
        /// <param name="text">The block of text to write.</param>
        /// <param name="position">The position at which to draw the text.</param>
        /// <param name="wrapWidth">The width at which to wrap the text.</param>
        /// <param name="color">The text color.</param>
        /// <param name="bold">Whether to draw bold text.</param>
        /// <param name="scale">The font scale.</param>
        /// <returns>Returns the text dimensions.</returns>
        public static Vector2 DrawTextBlock(this SpriteBatch batch, SpriteFont font, string text, Vector2 position, float wrapWidth, Color? color = null, bool bold = false, float scale = 1)
        {
            return batch.DrawTextBlock(font, new IFormattedText[] { new FormattedText(text, color, bold) }, position, wrapWidth, scale);
        }

        /// <summary>Draw a block of text to the screen with the specified wrap width.</summary>
        /// <param name="batch">The sprite batch.</param>
        /// <param name="font">The sprite font.</param>
        /// <param name="text">The block of text to write.</param>
        /// <param name="position">The position at which to draw the text.</param>
        /// <param name="wrapWidth">The width at which to wrap the text.</param>
        /// <param name="scale">The font scale.</param>
        /// <returns>Returns the text dimensions.</returns>
        public static Vector2 DrawTextBlock(this SpriteBatch batch, SpriteFont font, IEnumerable<IFormattedText> text, Vector2 position, float wrapWidth, float scale = 1)
        {
            if (text == null)
                return new Vector2(0, 0);

            // track draw values
            float xOffset = 0;
            float yOffset = 0;
            float lineHeight = font.MeasureString("ABC").Y * scale;
            float spaceWidth = DrawHelper.GetSpaceWidth(font) * scale;
            float blockWidth = 0;
            float blockHeight = lineHeight;

            // draw text snippets
            foreach (IFormattedText snippet in text)
            {
                if (snippet?.Text == null)
                    continue;

                // get word list
                List<string> words = new List<string>();
                foreach (string word in snippet.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    // split on newlines
                    string wordPart = word;
                    int newlineIndex;
                    while ((newlineIndex = wordPart.IndexOf(Environment.NewLine, StringComparison.InvariantCulture)) >= 0)
                    {
                        if (newlineIndex == 0)
                        {
                            words.Add(Environment.NewLine);
                            wordPart = wordPart.Substring(Environment.NewLine.Length);
                        }
                        else if (newlineIndex > 0)
                        {
                            words.Add(wordPart.Substring(0, newlineIndex));
                            words.Add(Environment.NewLine);
                            wordPart = wordPart.Substring(newlineIndex + Environment.NewLine.Length);
                        }
                    }

                    // add remaining word (after newline split)
                    if (wordPart.Length > 0)
                        words.Add(wordPart);
                }

                // draw words to screen
                foreach (string word in words)
                {
                    // check wrap width
                    float wordWidth = font.MeasureString(word).X * scale;
                    if (word == Environment.NewLine || ((wordWidth + xOffset) > wrapWidth && (int)xOffset != 0))
                    {
                        xOffset = 0;
                        yOffset += lineHeight;
                        blockHeight += lineHeight;
                    }
                    if (word == Environment.NewLine)
                        continue;

                    // draw text
                    Vector2 wordPosition = new Vector2(position.X + xOffset, position.Y + yOffset);
                    if (snippet.Bold)
                        Utility.drawBoldText(batch, word, font, wordPosition, snippet.Color ?? Color.Black, scale);
                    else
                        batch.DrawString(font, word, wordPosition, snippet.Color ?? Color.Black, 0, Vector2.Zero, scale, SpriteEffects.None, 1);

                    // update draw values
                    if (xOffset + wordWidth > blockWidth)
                        blockWidth = xOffset + wordWidth;
                    xOffset += wordWidth + spaceWidth;
                }
            }

            // return text position & dimensions
            return new Vector2(blockWidth, blockHeight);
        }
    }
}
