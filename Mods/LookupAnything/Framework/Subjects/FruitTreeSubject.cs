using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.DebugFields;
using Pathoschild.Stardew.LookupAnything.Framework.Fields;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.LookupAnything.Framework.Subjects
{
    /// <summary>Describes a non-fruit tree.</summary>
    internal class FruitTreeSubject : BaseSubject
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying target.</summary>
        private readonly FruitTree Target;

        /// <summary>The tree's tile position.</summary>
        private readonly Vector2 Tile;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="tree">The lookup target.</param>
        /// <param name="tile">The tree's tile position.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        public FruitTreeSubject(GameHelper gameHelper, FruitTree tree, Vector2 tile, ITranslationHelper translations)
            : base(gameHelper, translations.Get(L10n.FruitTree.Name, new { fruitName = gameHelper.GetObjectBySpriteIndex(tree.indexOfFruit.Value).DisplayName }), null, translations.Get(L10n.Types.FruitTree), translations)
        {
            this.Target = tree;
            this.Tile = tile;
        }

        /// <summary>Get the data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        /// <remarks>Tree growth algorithm reverse engineered from <see cref="FruitTree.dayUpdate"/>.</remarks>
        public override IEnumerable<ICustomField> GetData(Metadata metadata)
        {
            FruitTree tree = this.Target;

            // get basic info
            bool isMature = tree.daysUntilMature.Value <= 0;
            bool isDead = tree.stump.Value;
            bool isStruckByLightning = tree.struckByLightningCountdown.Value > 0;

            // show next fruit
            if (isMature && !isDead)
            {
                string label = this.Translate(L10n.FruitTree.NextFruit);
                if (isStruckByLightning)
                    yield return new GenericField(this.GameHelper, label, this.Translate(L10n.FruitTree.NextFruitStruckByLightning, new { count = tree.struckByLightningCountdown }));
                else if (Game1.currentSeason != tree.fruitSeason.Value && !tree.GreenHouseTree)
                    yield return new GenericField(this.GameHelper, label, this.Translate(L10n.FruitTree.NextFruitOutOfSeason));
                else if (tree.fruitsOnTree.Value == FruitTree.maxFruitsOnTrees)
                    yield return new GenericField(this.GameHelper, label, this.Translate(L10n.FruitTree.NextFruitMaxFruit));
                else
                    yield return new GenericField(this.GameHelper, label, this.Translate(L10n.Generic.Tomorrow));
            }

            // show growth data
            if (!isMature)
            {
                SDate dayOfMaturity = SDate.Now().AddDays(tree.daysUntilMature.Value);
                string grownOnDateText = this.Translate(L10n.FruitTree.GrowthSummary, new { date = this.Stringify(dayOfMaturity) });
                string daysUntilGrownText = this.Text.GetPlural(tree.daysUntilMature.Value, L10n.Generic.Tomorrow, L10n.Generic.InXDays).Tokens(new { count = tree.daysUntilMature });
                string growthText = $"{grownOnDateText} ({daysUntilGrownText})";

                yield return new GenericField(this.GameHelper, this.Translate(L10n.FruitTree.NextFruit), this.Translate(L10n.FruitTree.NextFruitTooYoung));
                yield return new GenericField(this.GameHelper, this.Translate(L10n.FruitTree.Growth), growthText);
                if (this.HasAdjacentObjects(this.Tile))
                    yield return new GenericField(this.GameHelper, this.Translate(L10n.FruitTree.Complaints), this.Translate(L10n.FruitTree.ComplaintsAdjacentObjects));
            }
            else
            {
                // get quality schedule
                ItemQuality currentQuality = this.GetCurrentQuality(tree, metadata.Constants.FruitTreeQualityGrowthTime);
                if (currentQuality == ItemQuality.Iridium)
                    yield return new GenericField(this.GameHelper, this.Translate(L10n.FruitTree.Quality), this.Translate(L10n.FruitTree.QualityNow, new { quality = this.Translate(L10n.For(currentQuality)) }));
                else
                {
                    string[] summary = this
                        .GetQualitySchedule(tree, currentQuality, metadata.Constants.FruitTreeQualityGrowthTime)
                        .Select(entry =>
                        {
                            // read schedule
                            ItemQuality quality = entry.Key;
                            int daysLeft = entry.Value;
                            SDate date = SDate.Now().AddDays(daysLeft);
                            int yearOffset = date.Year - Game1.year;

                            // generate summary line
                            string qualityName = this.Translate(L10n.For(quality));

                            if (daysLeft <= 0)
                                return "-" + this.Translate(L10n.FruitTree.QualityNow, new { quality = qualityName });

                            string line;
                            if (yearOffset == 0)
                                line = $"-{this.Translate(L10n.FruitTree.QualityOnDate, new { quality = qualityName, date = this.Stringify(date) })}";
                            else if (yearOffset == 1)
                                line = $"-{this.Translate(L10n.FruitTree.QualityOnDateNextYear, new { quality = qualityName, date = this.Stringify(date) })}";
                            else
                                line = $"-{this.Translate(L10n.FruitTree.QualityOnDate, new { quality = qualityName, date = this.Text.Stringify(date, withYear: true), year = date.Year })}";

                            line += $" ({this.Text.GetPlural(daysLeft, L10n.Generic.Tomorrow, L10n.Generic.InXDays).Tokens(new { count = daysLeft })})";

                            return line;
                        })
                        .ToArray();

                    yield return new GenericField(this.GameHelper, this.Translate(L10n.FruitTree.Quality), string.Join(Environment.NewLine, summary));
                }
            }

            // show season
            yield return new GenericField(this.GameHelper, this.Translate(L10n.FruitTree.Season), this.Translate(L10n.FruitTree.SeasonSummary, new { season = this.Text.GetSeasonName(tree.fruitSeason.Value) }));
        }

        /// <summary>Get raw debug data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public override IEnumerable<IDebugField> GetDebugFields(Metadata metadata)
        {
            FruitTree target = this.Target;

            // pinned fields
            yield return new GenericDebugField("mature in", $"{target.daysUntilMature} days", pinned: true);
            yield return new GenericDebugField("growth stage", target.growthStage.Value, pinned: true);
            yield return new GenericDebugField("health", target.health.Value, pinned: true);

            // raw fields
            foreach (IDebugField field in this.GetDebugFieldsFrom(target))
                yield return field;
        }

        /// <summary>Draw the subject portrait (if available).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="size">The size of the portrait to draw.</param>
        /// <returns>Returns <c>true</c> if a portrait was drawn, else <c>false</c>.</returns>
        public override bool DrawPortrait(SpriteBatch spriteBatch, Vector2 position, Vector2 size)
        {
            this.Target.drawInMenu(spriteBatch, position, Vector2.Zero, 1, 1);
            return true;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Whether there are adjacent objects that prevent growth.</summary>
        /// <param name="position">The tree's position in the current location.</param>
        private bool HasAdjacentObjects(Vector2 position)
        {
            GameLocation location = Game1.currentLocation;
            return (
                from adjacentTile in Utility.getSurroundingTileLocationsArray(position)
                let isOccupied = location.isTileOccupied(adjacentTile)
                let isEmptyDirt = location.terrainFeatures.ContainsKey(adjacentTile) && location.terrainFeatures[adjacentTile] is HoeDirt && ((HoeDirt)location.terrainFeatures[adjacentTile])?.crop == null
                select isOccupied && !isEmptyDirt
            ).Any(p => p);
        }

        /// <summary>Get the fruit quality produced by a tree.</summary>
        /// <param name="tree">The fruit tree.</param>
        /// <param name="daysPerQuality">The number of days before the tree begins producing a higher quality.</param>
        private ItemQuality GetCurrentQuality(FruitTree tree, int daysPerQuality)
        {
            int maturityLevel = Math.Max(0, Math.Min(3, -tree.daysUntilMature.Value / daysPerQuality));
            switch (maturityLevel)
            {
                case 0:
                    return ItemQuality.Normal;
                case 1:
                    return ItemQuality.Silver;
                case 2:
                    return ItemQuality.Gold;
                case 3:
                    return ItemQuality.Iridium;
                default:
                    throw new NotSupportedException($"Unexpected quality level {maturityLevel}.");
            }
        }

        /// <summary>Get a schedule indicating when a fruit tree will begin producing higher-quality fruit.</summary>
        /// <param name="tree">The fruit tree.</param>
        /// <param name="currentQuality">The current quality produced by the tree.</param>
        /// <param name="daysPerQuality">The number of days before the tree begins producing a higher quality.</param>
        private IEnumerable<KeyValuePair<ItemQuality, int>> GetQualitySchedule(FruitTree tree, ItemQuality currentQuality, int daysPerQuality)
        {
            if (tree.daysUntilMature.Value > 0)
                yield break; // not mature yet

            // yield current
            yield return new KeyValuePair<ItemQuality, int>(currentQuality, 0);

            // yield future qualities
            int dayOffset = daysPerQuality - Math.Abs(tree.daysUntilMature.Value % daysPerQuality);
            foreach (ItemQuality futureQuality in new[] { ItemQuality.Silver, ItemQuality.Gold, ItemQuality.Iridium })
            {
                if (currentQuality >= futureQuality)
                    continue;

                yield return new KeyValuePair<ItemQuality, int>(futureQuality, dayOffset);
                dayOffset += daysPerQuality;
            }
        }
    }
}
