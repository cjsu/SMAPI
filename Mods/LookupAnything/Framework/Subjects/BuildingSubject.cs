using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.DebugFields;
using Pathoschild.Stardew.LookupAnything.Framework.Fields;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Monsters;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.LookupAnything.Framework.Subjects
{
    /// <summary>Describes a constructed building.</summary>
    internal class BuildingSubject : BaseSubject
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private game code.</summary>
        private readonly IReflectionHelper Reflection;

        /// <summary>The lookup target.</summary>
        private readonly Building Target;

        /// <summary>The building's source rectangle in its spritesheet.</summary>
        private readonly Rectangle SourceRectangle;

        /// <summary>Provides metadata that's not available from the game data directly.</summary>
        private readonly Metadata Metadata;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="building">The lookup target.</param>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        /// <param name="sourceRectangle">The building's source rectangle in its spritesheet.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        /// <param name="reflectionHelper">Simplifies access to private game code.</param>
        public BuildingSubject(GameHelper gameHelper, Metadata metadata, Building building, Rectangle sourceRectangle, ITranslationHelper translations, IReflectionHelper reflectionHelper)
            : base(gameHelper, building.buildingType.Value, null, translations.Get(L10n.Types.Building), translations)
        {
            // init
            this.Metadata = metadata;
            this.Reflection = reflectionHelper;
            this.Target = building;
            this.SourceRectangle = sourceRectangle;

            // get name/description from blueprint if available
            try
            {
                BluePrint blueprint = new BluePrint(building.buildingType.Value);
                this.Name = blueprint.displayName;
                this.Description = blueprint.description;
            }
            catch (ContentLoadException)
            {
                // use default values
            }
        }

        /// <summary>Get the data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public override IEnumerable<ICustomField> GetData(Metadata metadata)
        {
            var text = this.Text;

            // get info
            Building building = this.Target;
            bool built = !building.isUnderConstruction();
            int? upgradeLevel = this.GetUpgradeLevel(building);

            // construction / upgrade
            if (!built || building.daysUntilUpgrade.Value > 0)
            {
                int daysLeft = building.isUnderConstruction() ? building.daysOfConstructionLeft.Value : building.daysUntilUpgrade.Value;
                SDate readyDate = SDate.Now().AddDays(daysLeft);
                yield return new GenericField(this.GameHelper, text.Get(L10n.Building.Construction), text.Get(L10n.Building.ConstructionSummary, new { date = readyDate }));
            }

            // owner
            Farmer owner = this.GetOwner();
            if (owner != null)
                yield return new LinkField(this.GameHelper, text.Get(L10n.Building.Owner), owner.Name, () => new FarmerSubject(this.GameHelper, owner, text, this.Reflection));
            else if (building.indoors.Value is Cabin)
                yield return new GenericField(this.GameHelper, text.Get(L10n.Building.Owner), text.Get(L10n.Building.OwnerNone));

            // stable horse
            if (built && building is Stable stable)
            {
                Horse horse = Utility.findHorse(stable.HorseId);
                if (horse != null)
                {
                    yield return new LinkField(this.GameHelper, text.Get(L10n.Building.Horse), horse.Name, () => new CharacterSubject(this.GameHelper, horse, TargetType.Horse, this.Metadata, text, this.Reflection));
                    yield return new GenericField(this.GameHelper, text.Get(L10n.Building.HorseLocation), text.Get(L10n.Building.HorseLocationSummary, new { location = horse.currentLocation.Name, x = horse.getTileX(), y = horse.getTileY() }));
                }
            }

            // animals
            if (built && building.indoors.Value is AnimalHouse animalHouse)
            {
                // animal counts
                yield return new GenericField(this.GameHelper, text.Get(L10n.Building.Animals), text.Get(L10n.Building.AnimalsSummary, new { count = animalHouse.animalsThatLiveHere.Count, max = animalHouse.animalLimit.Value }));

                // feed trough
                if ((building is Barn || building is Coop) && upgradeLevel >= 2)
                    yield return new GenericField(this.GameHelper, text.Get(L10n.Building.FeedTrough), text.Get(L10n.Building.FeedTroughAutomated));
                else
                {
                    this.GetFeedMetrics(animalHouse, out int totalFeedSpaces, out int filledFeedSpaces);
                    yield return new GenericField(this.GameHelper, text.Get(L10n.Building.FeedTrough), text.Get(L10n.Building.FeedTroughSummary, new { filled = filledFeedSpaces, max = totalFeedSpaces }));
                }
            }

            // slimes
            if (built && building.indoors.Value is SlimeHutch slimeHutch)
            {
                // slime count
                int slimeCount = slimeHutch.characters.OfType<GreenSlime>().Count();
                yield return new GenericField(this.GameHelper, text.Get(L10n.Building.Slimes), text.Get(L10n.Building.SlimesSummary, new { count = slimeCount, max = 20 }));

                // water trough
                yield return new GenericField(this.GameHelper, text.Get(L10n.Building.WaterTrough), text.Get(L10n.Building.WaterTroughSummary, new { filled = slimeHutch.waterSpots.Count(p => p), max = slimeHutch.waterSpots.Count }));
            }

            // upgrade level
            if (built)
            {
                var upgradeLevelSummary = this.GetUpgradeLevelSummary(building, upgradeLevel).ToArray();
                if (upgradeLevelSummary.Any())
                    yield return new CheckboxListField(this.GameHelper, text.Get(L10n.Building.Upgrades), upgradeLevelSummary);
            }

            // silo hay
            if (built && building.buildingType.Value == "Silo")
            {
                Farm farm = Game1.getFarm();
                int siloCount = Utility.numSilos();
                yield return new GenericField(
                    this.GameHelper,
                    text.Get(L10n.Building.StoredHay),
                    text.Get(siloCount == 1 ? L10n.Building.StoredHaySummaryOneSilo : L10n.Building.StoredHaySummaryMultipleSilos, new { hayCount = farm.piecesOfHay, siloCount = siloCount, maxHay = Math.Max(farm.piecesOfHay.Value, siloCount * 240) })
                );
            }

            if (built && building is JunimoHut hut)
            {
                yield return new GenericField(this.GameHelper, text.Get(L10n.Building.JunimoHarvestingEnabled), text.Stringify(!hut.noHarvest.Value));
                yield return new ItemIconListField(this.GameHelper, text.Get(L10n.Building.OutputReady), hut.output.Value?.items, showStackSize: true);
            }

            // mill output
            if (built && building is Mill mill)
            {
                yield return new ItemIconListField(this.GameHelper, text.Get(L10n.Building.OutputProcessing), mill.input.Value?.items, showStackSize: true);
                yield return new ItemIconListField(this.GameHelper, text.Get(L10n.Building.OutputReady), mill.output.Value?.items, showStackSize: true);
            }
        }

        /// <summary>Get raw debug data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public override IEnumerable<IDebugField> GetDebugFields(Metadata metadata)
        {
            Building target = this.Target;

            // pinned fields
            yield return new GenericDebugField("building type", target.buildingType.Value, pinned: true);
            yield return new GenericDebugField("days of construction left", target.daysOfConstructionLeft.Value, pinned: true);
            yield return new GenericDebugField("name of indoors", target.nameOfIndoors, pinned: true);

            // raw fields
            foreach (IDebugField field in this.GetDebugFieldsFrom(target))
                yield return field;
        }

        /// <summary>Draw the subject portrait (if available).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="size">The size of the portrait to draw.</param>
        /// <returns>Returns <c>true</c> if a portrait was drawn, else <c>false</c>.</returns>
        /// <remarks>Derived from <see cref="Building.drawInMenu"/>, modified to draw within the target size.</remarks>
        public override bool DrawPortrait(SpriteBatch spriteBatch, Vector2 position, Vector2 size)
        {
            Building target = this.Target;
            spriteBatch.Draw(target.texture.Value, position, this.SourceRectangle, target.color.Value, 0.0f, Vector2.Zero, size.X / this.SourceRectangle.Width, SpriteEffects.None, 0.89f);
            return true;
        }


        /*********
        ** Private fields
        *********/
        /// <summary>Get the building owner, if any.</summary>
        private Farmer GetOwner()
        {
            Building target = this.Target;

            // stable
            if (target is Stable stable)
            {
                long ownerID = stable.owner.Value;
                return Game1.getFarmerMaybeOffline(ownerID);
            }

            // cabin
            if (this.Target.indoors.Value is Cabin cabin)
                return cabin.owner;

            return null;
        }

        /// <summary>Get the upgrade level for a building, if applicable.</summary>
        /// <param name="building">The building to check.</param>
        private int? GetUpgradeLevel(Building building)
        {
            // barn
            if (building is Barn barn && int.TryParse(barn.nameOfIndoorsWithoutUnique.Substring("Barn".Length), out int barnUpgradeLevel))
                return barnUpgradeLevel - 1; // Barn2 is first upgrade

            // cabin
            if (building.indoors.Value is Cabin cabin)
                return cabin.upgradeLevel;

            // coop
            if (building is Coop coop && int.TryParse(coop.nameOfIndoorsWithoutUnique.Substring("Coop".Length), out int coopUpgradeLevel))
                return coopUpgradeLevel - 1; // Coop2 is first upgrade

            return null;
        }

        /// <summary>Get the feed metrics for an animal building.</summary>
        /// <param name="building">The animal building to check.</param>
        /// <param name="total">The total number of feed trough spaces.</param>
        /// <param name="filled">The number of feed trough spaces which contain hay.</param>
        private void GetFeedMetrics(AnimalHouse building, out int total, out int filled)
        {
            var map = building.Map;
            total = 0;
            filled = 0;

            for (int x = 0; x < map.Layers[0].LayerWidth; x++)
            {
                for (int y = 0; y < map.Layers[0].LayerHeight; y++)
                {
                    if (building.doesTileHaveProperty(x, y, "Trough", "Back") != null)
                    {
                        total++;
                        if (building.objects.TryGetValue(new Vector2(x, y), out SObject obj) && obj.ParentSheetIndex == 178)
                            filled++;
                    }
                }
            }
        }

        /// <summary>Get the upgrade levels for a building, for use with a checkbox field.</summary>
        /// <param name="building">The building to check.</param>
        /// <param name="upgradeLevel">The current upgrade level, if applicable.</param>
        private IEnumerable<KeyValuePair<IFormattedText[], bool>> GetUpgradeLevelSummary(Building building, int? upgradeLevel)
        {
            // barn
            if (building is Barn)
            {
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesBarn0)) },
                    value: true
                );
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesBarn1)) },
                    value: upgradeLevel >= 1
                );
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesBarn2)) },
                    value: upgradeLevel >= 2
                );
            }

            // cabin
            else if (building.indoors.Value is Cabin)
            {
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesCabin0)) },
                    value: true
                );
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesCabin1)) },
                    value: upgradeLevel >= 1
                );
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesCabin2)) },
                    value: upgradeLevel >= 2
                );
            }

            // coop
            else if (building is Coop)
            {
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesCoop0)) },
                    value: true
                );
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesCoop1)) },
                    value: upgradeLevel >= 1
                );
                yield return new KeyValuePair<IFormattedText[], bool>(
                    key: new IFormattedText[] { new FormattedText(this.Text.Get(L10n.Building.UpgradesCoop2)) },
                    value: upgradeLevel >= 2
                );
            }
        }
    }
}
