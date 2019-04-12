using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using StardewModdingAPI;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field which shows experience points for a skill.</summary>
    /// <remarks>Skill calculations reverse-engineered from <see cref="StardewValley.Farmer.checkForLevelGain"/>.</remarks>
    internal class SkillBarField : PercentageBarField
    {
        /*********
        ** Fields
        *********/
        /// <summary>The experience points needed for each skill level.</summary>
        private readonly int[] SkillPointsPerLevel;

        /// <summary>Provides translations stored in the mod folder.</summary>
        private readonly ITranslationHelper Translations;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="experience">The current progress value.</param>
        /// <param name="maxSkillPoints">The maximum experience points for a skill.</param>
        /// <param name="skillPointsPerLevel">The experience points needed for each skill level.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        public SkillBarField(GameHelper gameHelper, string label, int experience, int maxSkillPoints, int[] skillPointsPerLevel, ITranslationHelper translations)
            : base(gameHelper, label, experience, maxSkillPoints, Color.Green, Color.Gray, null)
        {
            this.SkillPointsPerLevel = skillPointsPerLevel;
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
            int[] pointsPerLevel = this.SkillPointsPerLevel;

            // generate text
            int nextLevelExp = pointsPerLevel.FirstOrDefault(p => p - this.CurrentValue > 0);
            int pointsForNextLevel = nextLevelExp > 0 ? nextLevelExp - this.CurrentValue : 0;
            int currentLevel = nextLevelExp > 0 ? Array.IndexOf(pointsPerLevel, nextLevelExp) : pointsPerLevel.Length;
            string text = pointsForNextLevel > 0
                ? this.Translations.Get(L10n.Player.SkillProgress, new { level = currentLevel, expNeeded = pointsForNextLevel })
                : this.Translations.Get(L10n.Player.SkillProgressLast, new { level = currentLevel });

            // draw bars
            const int barWidth = 25;
            float leftOffset = 0;
            int barHeight = 0;
            foreach (int levelExp in pointsPerLevel)
            {
                float progress = Math.Min(1f, this.CurrentValue / (levelExp * 1f));
                Vector2 barSize = this.DrawBar(spriteBatch, position + new Vector2(leftOffset, 0), progress, this.FilledColor, this.EmptyColor, barWidth);
                barHeight = (int)barSize.Y;
                leftOffset += barSize.X + 2;
            }

            // draw text
            Vector2 textSize = spriteBatch.DrawTextBlock(font, text, position + new Vector2(leftOffset, 0), wrapWidth - leftOffset);
            return new Vector2(leftOffset + textSize.X, Math.Max(barHeight, textSize.Y));
        }
    }
}
