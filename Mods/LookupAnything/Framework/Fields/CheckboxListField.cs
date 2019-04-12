using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Components;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field which shows a list of checkbox values.</summary>
    internal class CheckboxListField : GenericField
    {
        /*********
        ** Fields
        *********/
        /// <summary>The checkbox values to display.</summary>
        private readonly KeyValuePair<IFormattedText[], bool>[] Checkboxes;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="checkboxes">The checkbox labels and values to display.</param>
        public CheckboxListField(GameHelper gameHelper, string label, IEnumerable<KeyValuePair<IFormattedText[], bool>> checkboxes)
            : base(gameHelper, label, hasValue: true)
        {
            this.Checkboxes = checkboxes.ToArray();
        }

        /// <summary>Draw the value (or return <c>null</c> to render the <see cref="GenericField.Value"/> using the default format).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="font">The recommended font.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="wrapWidth">The maximum width before which content should be wrapped.</param>
        /// <returns>Returns the drawn dimensions, or <c>null</c> to draw the <see cref="GenericField.Value"/> using the default format.</returns>
        public override Vector2? DrawValue(SpriteBatch spriteBatch, SpriteFont font, Vector2 position, float wrapWidth)
        {
            float topOffset = 0;
            float checkboxSize = Sprites.Icons.FilledCheckbox.Width * (Game1.pixelZoom / 2);
            float lineHeight = Math.Max(checkboxSize, Game1.smallFont.MeasureString("ABC").Y);
            float checkboxOffset = (lineHeight - checkboxSize) / 2;

            foreach (KeyValuePair<IFormattedText[], bool> entry in this.Checkboxes)
            {
                // draw icon
                spriteBatch.Draw(
                    texture: Sprites.Icons.Sheet,
                    position: new Vector2(position.X, position.Y + topOffset + checkboxOffset),
                    sourceRectangle: entry.Value ? Sprites.Icons.FilledCheckbox : Sprites.Icons.EmptyCheckbox,
                    color: Color.White,
                    rotation: 0,
                    origin: Vector2.Zero,
                    scale: checkboxSize / Sprites.Icons.FilledCheckbox.Width,
                    effects: SpriteEffects.None,
                    layerDepth: 1f
                );

                // draw text
                Vector2 textSize = spriteBatch.DrawTextBlock(Game1.smallFont, entry.Key, new Vector2(position.X + checkboxSize + 7, position.Y + topOffset), wrapWidth - checkboxSize - 7);

                // update offset
                topOffset += Math.Max(checkboxSize, textSize.Y);
            }

            return new Vector2(wrapWidth, topOffset);
        }
    }
}
