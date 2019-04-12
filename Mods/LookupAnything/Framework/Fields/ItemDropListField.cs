using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.Data;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field which shows a list of item drops.</summary>
    internal class ItemDropListField : GenericField
    {
        /*********
        ** Fields
        *********/
        /// <summary>The possible drops.</summary>
        private readonly Tuple<ItemDropData, SObject, SpriteInfo>[] Drops;

        /// <summary>The text to display if there are no items.</summary>
        private readonly string DefaultText;

        /// <summary>Provides translations stored in the mod folder.</summary>
        private readonly ITranslationHelper Translations;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="drops">The possible drops.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        /// <param name="defaultText">The text to display if there are no items (or <c>null</c> to hide the field).</param>
        public ItemDropListField(GameHelper gameHelper, string label, IEnumerable<ItemDropData> drops, ITranslationHelper translations, string defaultText = null)
            : base(gameHelper, label)
        {
            this.Drops = this
                .GetEntries(drops, gameHelper)
                .OrderByDescending(p => p.Item1.Probability)
                .ThenBy(p => p.Item2.DisplayName)
                .ToArray();
            this.DefaultText = defaultText;
            this.HasValue = defaultText != null || this.Drops.Any();
            this.Translations = translations;
        }

        /// <summary>Draw the value (or return <c>null</c> to render the <see cref="GenericField.Value"/> using the default format).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="font">The recommended font.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="wrapWidth">The maximum width before which content should be wrapped.</param>
        /// <returns>Returns the drawn dimensions, or <c>null</c> to draw the <see cref="GenericField.Value"/> using the default format.</returns>
        public override Vector2? DrawValue(SpriteBatch spriteBatch, SpriteFont font, Vector2 position, float wrapWidth)
        {
            if (!this.Drops.Any())
                return spriteBatch.DrawTextBlock(font, this.DefaultText, position, wrapWidth);

            // get icon size
            Vector2 iconSize = new Vector2(font.MeasureString("ABC").Y);

            // list drops
            bool canReroll = Game1.player.isWearingRing(Ring.burglarsRing);
            float height = 0;
            foreach (var entry in this.Drops)
            {
                // get data
                ItemDropData drop = entry.Item1;
                SObject item = entry.Item2;
                SpriteInfo sprite = entry.Item3;
                bool isGuaranteed = drop.Probability > .99f;

                // draw icon
                spriteBatch.DrawSpriteWithin(sprite, position.X, position.Y + height, iconSize, isGuaranteed ? Color.White : Color.White * 0.5f);

                // draw text
                string text = isGuaranteed ? item.DisplayName : this.Translations.Get(L10n.Generic.PercentChanceOf, new { percent = Math.Round(drop.Probability, 4) * 100, label = item.DisplayName });
                if (drop.MaxDrop > 1)
                    text += " (" + this.Translations.Get(L10n.Generic.Range, new { min = 1, max = drop.MaxDrop }) + ")";
                Vector2 textSize = spriteBatch.DrawTextBlock(font, text, position + new Vector2(iconSize.X + 5, height + 5), wrapWidth, isGuaranteed ? Color.Black : Color.Gray);

                // cross out item if it definitely won't drop
                if (!isGuaranteed && !canReroll)
                    spriteBatch.DrawLine(position.X + iconSize.X + 5, position.Y + height + iconSize.Y / 2, new Vector2(textSize.X, 1), Color.Gray);

                height += textSize.Y + 5;
            }

            // return size
            return new Vector2(wrapWidth, height);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the internal drop list entries.</summary>
        /// <param name="drops">The possible drops.</param>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        private IEnumerable<Tuple<ItemDropData, SObject, SpriteInfo>> GetEntries(IEnumerable<ItemDropData> drops, GameHelper gameHelper)
        {
            foreach (ItemDropData drop in drops)
            {
                SObject item = this.GameHelper.GetObjectBySpriteIndex(drop.ItemID);
                SpriteInfo sprite = gameHelper.GetSprite(item);
                yield return Tuple.Create(drop, item, sprite);
            }
        }
    }
}
