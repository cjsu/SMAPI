using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field which shows a list of linked item names with icons.</summary>
    internal class ItemIconListField : GenericField
    {
        /*********
        ** Fields
        *********/
        /// <summary>The items to draw.</summary>
        private readonly Tuple<Item, SpriteInfo>[] Items;

        /// <summary>Whether to draw the stack size on the item icon.</summary>
        private readonly bool ShowStackSize;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="items">The items to display.</param>
        /// <param name="showStackSize">Whether to draw the stack size on the item icon.</param>
        public ItemIconListField(GameHelper gameHelper, string label, IEnumerable<Item> items, bool showStackSize)
            : base(gameHelper, label, hasValue: items != null)
        {
            if (items == null)
                return;

            this.Items = items.Where(p => p != null).Select(item => Tuple.Create(item, gameHelper.GetSprite(item))).ToArray();
            this.HasValue = this.Items.Any();
            this.ShowStackSize = showStackSize;
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

            // draw list
            const int padding = 5;
            int topOffset = 0;
            foreach (Tuple<Item, SpriteInfo> entry in this.Items)
            {
                Item item = entry.Item1;
                SpriteInfo sprite = entry.Item2;

                // draw icon
                spriteBatch.DrawSpriteWithin(sprite, position.X, position.Y + topOffset, iconSize);
                if (this.ShowStackSize && item.Stack > 1)
                {
                    float scale = 2f; //sprite.SourceRectangle.Width / iconSize.X;
                    Vector2 sizePos = position + new Vector2(iconSize.X - Utility.getWidthOfTinyDigitString(item.Stack, scale), iconSize.Y + topOffset - 6f * scale);
                    Utility.drawTinyDigits(item.Stack, spriteBatch, sizePos, scale: scale, layerDepth: 1f, Color.White);
                }

                Vector2 textSize = spriteBatch.DrawTextBlock(font, item.DisplayName, position + new Vector2(iconSize.X + padding, topOffset), wrapWidth);

                topOffset += (int)Math.Max(iconSize.Y, textSize.Y) + padding;
            }

            // return size
            return new Vector2(wrapWidth, topOffset + padding);
        }
    }
}
