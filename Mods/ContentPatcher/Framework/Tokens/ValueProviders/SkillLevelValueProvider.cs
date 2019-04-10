using System;
using System.Collections.Generic;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.Constants;
using Pathoschild.Stardew.Common.Utilities;
using StardewValley;

namespace ContentPatcher.Framework.Tokens.ValueProviders
{
    /// <summary>A value provider for the player's skill levels.</summary>
    internal class SkillLevelValueProvider : BaseValueProvider
    {
        /*********
        ** Fields
        *********/
        /// <summary>Get whether the player data is available in the current context.</summary>
        private readonly Func<bool> IsPlayerDataAvailable;

        /// <summary>The player's current skill levels.</summary>
        private readonly IDictionary<Skill, int> SkillLevels = new Dictionary<Skill, int>();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SkillLevelValueProvider(Func<bool> isPlayerDataAvailable)
            : base(ConditionType.SkillLevel, canHaveMultipleValuesForRoot: true)
        {
            this.IsPlayerDataAvailable = isPlayerDataAvailable;
            this.EnableInputArguments(required: false, canHaveMultipleValues: false);
        }

        /// <summary>Update the underlying values.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the values changed.</returns>
        public override void UpdateContext(IContext context)
        {
            this.SkillLevels.Clear();
            this.IsValidInContext = this.IsPlayerDataAvailable();
            if (this.IsValidInContext)
            {
                this.SkillLevels[Skill.Combat] = Game1.player.CombatLevel;
                this.SkillLevels[Skill.Farming] = Game1.player.FarmingLevel;
                this.SkillLevels[Skill.Fishing] = Game1.player.FishingLevel;
                this.SkillLevels[Skill.Foraging] = Game1.player.ForagingLevel;
                this.SkillLevels[Skill.Luck] = Game1.player.LuckLevel;
                this.SkillLevels[Skill.Mining] = Game1.player.MiningLevel;
            }
        }

        /// <summary>Get the set of valid input arguments if restricted, or an empty collection if unrestricted.</summary>
        public override InvariantHashSet GetValidInputs()
        {
            return new InvariantHashSet(Enum.GetNames(typeof(Skill)));
        }

        /// <summary>Get the current values.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override IEnumerable<string> GetValues(string input)
        {
            this.AssertInputArgument(input);

            if (input != null)
            {
                if (this.TryParseEnum(input, out Skill skill) && this.SkillLevels.TryGetValue(skill, out int level))
                    yield return level.ToString();
            }
            else
            {
                foreach (var pair in this.SkillLevels)
                    yield return $"{pair.Key}:{pair.Value}";
            }
        }
    }
}
