using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field which shows an item icon.</summary>
    internal class ItemIconField : GenericField
    {
        /*********
        ** Fields
        *********/
        /// <summary>The item icon to draw.</summary>
        private readonly SpriteInfo Sprite;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="item">The item for which to display an icon.</param>
        /// <param name="text">The text to display (if not the item name).</param>
        public ItemIconField(GameHelper gameHelper, string label, Item item, string text = null)
            : base(gameHelper, label, hasValue: item != null)
        {
            this.Sprite = gameHelper.GetSprite(item);
            if (item != null)
            {
                this.Value = !string.IsNullOrWhiteSpace(text)
                    ? this.FormatValue(text)
                    : this.FormatValue(item.DisplayName);
            }
        }

        /// <summary>Draw the value (or return <c>null</c> to render the <see cref="GenericField.Value"/> using the default format).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="font">The recommended font.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="wrapWidth">The maximum width before which content should be wrapped.</param>
        /// <returns>Returns the drawn dimensions, or <c>null</c> to draw the <see cref="GenericField.Value"/> using the default format.</returns>
        public override Vector2? DrawValue(SpriteBatch spriteBatch, SpriteFont font, Vector2 position, float wrapWidth)
        {
            // get icon size
            float textHeight = font.MeasureString("ABC").Y;
            Vector2 iconSize = new Vector2(textHeight);

            // draw icon & text
            spriteBatch.DrawSpriteWithin(this.Sprite, position.X, position.Y, iconSize);
            Vector2 textSize = spriteBatch.DrawTextBlock(font, this.Value, position + new Vector2(iconSize.X + 5, 5), wrapWidth);

            // return size
            return new Vector2(wrapWidth, textSize.Y + 5);
        }
    }
}
