using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.DebugFields;
using Pathoschild.Stardew.LookupAnything.Framework.Fields;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Subjects
{
    /// <summary>Describes a farm animal.</summary>
    internal class FarmAnimalSubject : BaseSubject
    {
        /*********
        ** Fields
        *********/
        /// <summary>The lookup target.</summary>
        private readonly FarmAnimal Target;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="animal">The lookup target.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        /// <remarks>Reverse engineered from <see cref="FarmAnimal"/>.</remarks>
        public FarmAnimalSubject(GameHelper gameHelper, FarmAnimal animal, ITranslationHelper translations)
            : base(gameHelper, animal.displayName, null, animal.type.Value, translations)
        {
            this.Target = animal;
        }

        /// <summary>Get the data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public override IEnumerable<ICustomField> GetData(Metadata metadata)
        {
            FarmAnimal animal = this.Target;

            // calculate maturity
            bool isFullyGrown = animal.age.Value >= animal.ageWhenMature.Value;
            int daysUntilGrown = 0;
            SDate dayOfMaturity = null;
            if (!isFullyGrown)
            {
                daysUntilGrown = animal.ageWhenMature.Value - animal.age.Value;
                dayOfMaturity = SDate.Now().AddDays(daysUntilGrown);
            }

            // yield fields
            yield return new CharacterFriendshipField(this.GameHelper, this.Translate(L10n.Animal.Love), this.GameHelper.GetFriendshipForAnimal(Game1.player, animal, metadata), this.Text);
            yield return new PercentageBarField(this.GameHelper, this.Translate(L10n.Animal.Happiness), animal.happiness.Value, byte.MaxValue, Color.Green, Color.Gray, this.Translate(L10n.Generic.Percent, new { percent = Math.Round(animal.happiness.Value / (metadata.Constants.AnimalMaxHappiness * 1f) * 100) }));
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Animal.Mood), animal.getMoodMessage());
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Animal.Complaints), this.GetMoodReason(animal));
            yield return new ItemIconField(this.GameHelper, this.Translate(L10n.Animal.ProduceReady), animal.currentProduce.Value > 0 ? this.GameHelper.GetObjectBySpriteIndex(animal.currentProduce.Value) : null);
            if (!isFullyGrown)
                yield return new GenericField(this.GameHelper, this.Translate(L10n.Animal.Growth), $"{this.Translate(L10n.Generic.Days, new { count = daysUntilGrown })} ({this.Stringify(dayOfMaturity)})");
            yield return new GenericField(this.GameHelper, this.Translate(L10n.Animal.SellsFor), GenericField.GetSaleValueString(animal.getSellPrice(), 1, this.Text));
        }

        /// <summary>Get raw debug data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public override IEnumerable<IDebugField> GetDebugFields(Metadata metadata)
        {
            FarmAnimal target = this.Target;

            // pinned fields
            yield return new GenericDebugField("age", $"{target.age} days", pinned: true);
            yield return new GenericDebugField("friendship", $"{target.friendshipTowardFarmer} (max {metadata.Constants.AnimalMaxHappiness})", pinned: true);
            yield return new GenericDebugField("fullness", this.Stringify(target.fullness.Value), pinned: true);
            yield return new GenericDebugField("happiness", this.Stringify(target.happiness.Value), pinned: true);

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
            FarmAnimal animal = this.Target;
            animal.Sprite.draw(spriteBatch, position, 1, 0, 0, Color.White, scale: size.X / animal.Sprite.getWidth());
            return true;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a short explanation for the animal's current mod.</summary>
        /// <param name="animal">The farm animal.</param>
        private string GetMoodReason(FarmAnimal animal)
        {
            List<string> factors = new List<string>();

            // winter without heat
            if (Game1.IsWinter && Game1.currentLocation.numberOfObjectsWithName(Constant.ItemNames.Heater) <= 0)
                factors.Add(this.Translate(L10n.Animal.ComplaintsNoHeater));

            // mood
            switch (animal.moodMessage.Value)
            {
                case FarmAnimal.newHome:
                    factors.Add(this.Translate(L10n.Animal.ComplaintsNewHome));
                    break;
                case FarmAnimal.hungry:
                    factors.Add(this.Translate(L10n.Animal.ComplaintsHungry));
                    break;
                case FarmAnimal.disturbedByDog:
                    factors.Add(this.Translate(L10n.Animal.ComplaintsWildAnimalAttack));
                    break;
                case FarmAnimal.leftOutAtNight:
                    factors.Add(this.Translate(L10n.Animal.ComplaintsLeftOut));
                    break;
            }

            // not pet
            if (!animal.wasPet.Value)
                factors.Add(this.Translate(L10n.Animal.ComplaintsNotPetted));

            // return factors
            return string.Join(", ", factors);
        }
    }
}
