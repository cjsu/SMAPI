using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.DebugFields;
using Pathoschild.Stardew.LookupAnything.Framework.Fields;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SFarmer = StardewValley.Farmer;

namespace Pathoschild.Stardew.LookupAnything.Framework.Subjects
{
    /// <summary>Describes a farmer (i.e. player).</summary>
    internal class FarmerSubject : BaseSubject
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private game code.</summary>
        private readonly IReflectionHelper Reflection;

        /// <summary>The lookup target.</summary>
        private readonly SFarmer Target;

        /// <summary>Whether this is being displayed on the load menu, before the save data is fully initialised.</summary>
        private readonly bool IsLoadMenu;

        ///// <summary>The raw save data for this player, if <see cref="IsLoadMenu"/> is true.</summary>
        //private readonly Lazy<XElement> RawSaveData;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="farmer">The lookup target.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        /// <param name="reflectionHelper">Simplifies access to private game code.</param>
        /// <param name="isLoadMenu">Whether this is being displayed on the load menu, before the save data is fully initialised.</param>
        public FarmerSubject(GameHelper gameHelper, SFarmer farmer, ITranslationHelper translations, IReflectionHelper reflectionHelper, bool isLoadMenu = false)
            : base(gameHelper, farmer.Name, null, translations.Get(L10n.Types.Player), translations)
        {
            this.Reflection = reflectionHelper;
            this.Target = farmer;
            this.IsLoadMenu = isLoadMenu;
            //this.RawSaveData = isLoadMenu
            //    ? new Lazy<XElement>(() => this.ReadSaveFile(farmer.slotName))
            //    : null;
        }

        /// <summary>Get the data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public override IEnumerable<ICustomField> GetData(Metadata metadata)
        {
            SFarmer target = this.Target;

            int maxSkillPoints = metadata.Constants.PlayerMaxSkillPoints;
            int[] skillPointsPerLevel = metadata.Constants.PlayerSkillPointsPerLevel;
            string luckSummary = this.Translate(L10n.Player.LuckSummary, new { percent = (Game1.dailyLuck >= 0 ? "+" : "") + Math.Round(Game1.dailyLuck * 100, 2) });

            yield return new GenericField(this.GameHelper, this.Translate(L10n.Player.Gender), this.Translate(target.IsMale ? L10n.Player.GenderMale : L10n.Player.GenderFemale));
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Player.FarmName), target.farmName.Value);
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Player.FarmMap), this.GetFarmType());
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Player.FavoriteThing), target.favoriteThing.Value);
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Player.Spouse), this.GetSpouseName());
            yield return new SkillBarField(this.GameHelper, this.Translate(L10n.Player.FarmingSkill), target.experiencePoints[SFarmer.farmingSkill], maxSkillPoints, skillPointsPerLevel, this.Text);
            yield return new SkillBarField(this.GameHelper, this.Translate(L10n.Player.MiningSkill), target.experiencePoints[SFarmer.miningSkill], maxSkillPoints, skillPointsPerLevel, this.Text);
            yield return new SkillBarField(this.GameHelper, this.Translate(L10n.Player.ForagingSkill), target.experiencePoints[SFarmer.foragingSkill], maxSkillPoints, skillPointsPerLevel, this.Text);
            yield return new SkillBarField(this.GameHelper, this.Translate(L10n.Player.FishingSkill), target.experiencePoints[SFarmer.fishingSkill], maxSkillPoints, skillPointsPerLevel, this.Text);
            yield return new SkillBarField(this.GameHelper, this.Translate(L10n.Player.CombatSkill), target.experiencePoints[SFarmer.combatSkill], maxSkillPoints, skillPointsPerLevel, this.Text);
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Player.Luck), $"{this.GetSpiritLuckMessage()}{Environment.NewLine}({luckSummary})");
        }

        /// <summary>Get raw debug data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public override IEnumerable<IDebugField> GetDebugFields(Metadata metadata)
        {
            SFarmer target = this.Target;

            // pinned fields
            yield return new GenericDebugField("immunity", target.immunity, pinned: true);
            yield return new GenericDebugField("resilience", target.resilience, pinned: true);
            yield return new GenericDebugField("magnetic radius", target.MagneticRadius, pinned: true);

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
            SFarmer target = this.Target;

            if (this.IsLoadMenu)
                target.FarmerRenderer.draw(spriteBatch, new FarmerSprite.AnimationFrame(0, 0, false, false), 0, new Rectangle(0, 0, 16, 32), position, Vector2.Zero, 0.8f, 2, Color.White, 0.0f, 1f, target);
            else
            {
                FarmerSprite sprite = target.FarmerSprite;
                target.FarmerRenderer.draw(spriteBatch, sprite.CurrentAnimationFrame, sprite.CurrentFrame, sprite.SourceRect, position, Vector2.Zero, 0.8f, Color.White, 0, 1f, target);
            }

            return true;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a summary of the player's luck today.</summary>
        /// <remarks>Derived from <see cref="GameLocation.answerDialogueAction"/>.</remarks>
        private string GetSpiritLuckMessage()
        {
            // inject daily luck if not loaded yet
            //if (this.IsLoadMenu)
            //{
            //    string rawDailyLuck = this.RawSaveData.Value?.Element("dailyLuck")?.Value;
            //    if (rawDailyLuck == null)
            //        return null;

            //    Game1.dailyLuck = double.Parse(rawDailyLuck);
            //}

            // get daily luck message
            TV tv = new TV();
            return this.Reflection.GetMethod(tv, "getFortuneForecast").Invoke<string>();
        }

        /// <summary>Get the human-readable farm type selected by the player.</summary>
        private string GetFarmType()
        {
            // get farm type
            int farmType = Game1.whichFarm;
            //if (this.IsLoadMenu)
            //{
            //    string rawType = this.RawSaveData.Value?.Element("whichFarm")?.Value;
            //    farmType = rawType != null ? int.Parse(rawType) : -1;
            //}

            // get type name
            switch (farmType)
            {
                case -1:
                    return null;

                case Farm.combat_layout:
                    return Game1.content.LoadString("Strings\\UI:Character_FarmCombat").Split('_').FirstOrDefault();
                case Farm.default_layout:
                    return Game1.content.LoadString("Strings\\UI:Character_FarmStandard").Split('_').FirstOrDefault();
                case Farm.forest_layout:
                    return Game1.content.LoadString("Strings\\UI:Character_FarmForaging").Split('_').FirstOrDefault();
                case Farm.mountains_layout:
                    return Game1.content.LoadString("Strings\\UI:Character_FarmMining").Split('_').FirstOrDefault();
                case Farm.riverlands_layout:
                    return Game1.content.LoadString("Strings\\UI:Character_FarmFishing").Split('_').FirstOrDefault();

                default:
                    return this.Translate(L10n.Player.FarmMapCustom);
            }
        }

        /// <summary>Get the player's spouse name, if they're married.</summary>
        /// <returns>Returns the spouse name, or <c>null</c> if they're not married.</returns>
        private string GetSpouseName()
        {
            if (this.IsLoadMenu)
                return this.Target.spouse;

            long? spousePlayerID = this.Target.team.GetSpouse(this.Target.UniqueMultiplayerID);
            SFarmer spousePlayer = spousePlayerID.HasValue ? Game1.getFarmerMaybeOffline(spousePlayerID.Value) : null;

            return spousePlayer?.displayName ?? Game1.player.getSpouse()?.displayName;
        }

        /// <summary>Load the raw save file as an XML document.</summary>
        /// <param name="slotName">The slot file to read.</param>
        //private XElement ReadSaveFile(string slotName)
        //{
        //    if (slotName == null)
        //        return null; // not available (e.g. farmhand)

        //    // get file
        //    FileInfo file = new FileInfo(Path.Combine(StardewModdingAPI.Constants.SavesPath, slotName, slotName));
        //    if (!file.Exists)
        //        return null;

        //    // read contents
        //    string text = File.ReadAllText(file.FullName);
        //    return XElement.Parse(text);
        //}
    }
}
