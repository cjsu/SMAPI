using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Components;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.Models;
using StardewModdingAPI;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field which shows friendship points.</summary>
    internal class CharacterFriendshipField : GenericField
    {
        /*********
        ** Fields
        *********/
        /// <summary>The player's current friendship data with the NPC.</summary>
        private readonly FriendshipModel Friendship;

        /// <summary>Provides translations stored in the mod folder.</summary>
        private readonly ITranslationHelper Translations;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="friendship">The player's current friendship data with the NPC.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        public CharacterFriendshipField(GameHelper gameHelper, string label, FriendshipModel friendship, ITranslationHelper translations)
            : base(gameHelper, label, hasValue: true)
        {
            this.Friendship = friendship;
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
            FriendshipModel friendship = this.Friendship;

            // draw status
            float leftOffset = 0;
            {
                string statusText = this.Translations.Get(L10n.For(friendship.Status));
                Vector2 textSize = spriteBatch.DrawTextBlock(font, statusText, new Vector2(position.X + leftOffset, position.Y), wrapWidth - leftOffset);
                leftOffset += textSize.X + DrawHelper.GetSpaceWidth(font);
            }

            // draw hearts
            for (int i = 0; i < friendship.TotalHearts; i++)
            {
                // get icon
                Color color;
                Rectangle icon;
                if (friendship.LockedHearts >= friendship.TotalHearts - i)
                {
                    icon = Sprites.Icons.FilledHeart;
                    color = Color.Black * 0.35f;
                }
                else if (i >= friendship.FilledHearts)
                {
                    icon = Sprites.Icons.EmptyHeart;
                    color = Color.White;
                }
                else
                {
                    icon = Sprites.Icons.FilledHeart;
                    color = Color.White;
                }

                // draw
                spriteBatch.DrawSprite(Sprites.Icons.Sheet, icon, position.X + leftOffset, position.Y, color, Game1.pixelZoom);
                leftOffset += Sprites.Icons.FilledHeart.Width * Game1.pixelZoom;
            }

            // draw stardrop (if applicable)
            if (friendship.HasStardrop)
            {
                leftOffset += 1;
                float zoom = (Sprites.Icons.EmptyHeart.Height / (Sprites.Icons.Stardrop.Height * 1f)) * Game1.pixelZoom;
                spriteBatch.DrawSprite(Sprites.Icons.Sheet, Sprites.Icons.Stardrop, position.X + leftOffset, position.Y, Color.White * 0.25f, zoom);
                leftOffset += Sprites.Icons.Stardrop.Width * zoom;
            }

            // get caption text
            string caption = null;
            if (this.Friendship.EmptyHearts == 0 && this.Friendship.LockedHearts > 0)
                caption = $"({this.Translations.Get(L10n.Npc.FriendshipNeedBouquet)})";
            else
            {
                int pointsToNext = this.Friendship.GetPointsToNext();
                if (pointsToNext > 0)
                    caption = $"({this.Translations.Get(L10n.Npc.FriendshipNeedPoints, new { count = pointsToNext })})";
            }

            // draw caption
            {
                float spaceSize = DrawHelper.GetSpaceWidth(font);
                Vector2 textSize = Vector2.Zero;
                if (caption != null)
                    textSize = spriteBatch.DrawTextBlock(font, caption, new Vector2(position.X + leftOffset + spaceSize, position.Y), wrapWidth - leftOffset);

                return new Vector2(Sprites.Icons.FilledHeart.Width * Game1.pixelZoom * this.Friendship.TotalHearts + textSize.X + spaceSize, Math.Max(Sprites.Icons.FilledHeart.Height * Game1.pixelZoom, textSize.Y));
            }
        }
    }
}
