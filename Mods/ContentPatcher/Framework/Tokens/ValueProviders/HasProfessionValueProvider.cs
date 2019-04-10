using System;
using System.Collections.Generic;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.Constants;
using Pathoschild.Stardew.Common.Utilities;
using StardewValley;

namespace ContentPatcher.Framework.Tokens.ValueProviders
{
    /// <summary>A value provider for the player's professions.</summary>
    internal class HasProfessionValueProvider : BaseValueProvider
    {
        /*********
        ** Fields
        *********/
        /// <summary>Get whether the player data is available in the current context.</summary>
        private readonly Func<bool> IsPlayerDataAvailable;

        /// <summary>The player's current professions.</summary>
        private readonly HashSet<Profession> Professions = new HashSet<Profession>();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="isPlayerDataAvailable">Get whether the player data is available in the current context.</param>
        public HasProfessionValueProvider(Func<bool> isPlayerDataAvailable)
            : base(ConditionType.HasProfession, canHaveMultipleValuesForRoot: true)
        {
            this.IsPlayerDataAvailable = isPlayerDataAvailable;
            this.EnableInputArguments(required: false, canHaveMultipleValues: false);
        }

        /// <summary>Update the underlying values.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the values changed.</returns>
        public override void UpdateContext(IContext context)
        {
            this.Professions.Clear();
            this.IsValidInContext = this.IsPlayerDataAvailable();
            if (this.IsValidInContext)
            {
                foreach (int professionID in Game1.player.professions)
                    this.Professions.Add((Profession)professionID);
            }
        }

        /// <summary>Get the allowed values for a token name (or <c>null</c> if any value is allowed).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this token, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override InvariantHashSet GetAllowedValues(string input)
        {
            return input != null
                ? InvariantHashSet.Boolean()
                : null;
        }

        /// <summary>Get the current values.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this token, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override IEnumerable<string> GetValues(string input)
        {
            this.AssertInputArgument(input);

            if (input != null)
            {
                bool hasProfession = this.TryParseEnum(input, out Profession profession, mustBeNamed: false) && this.Professions.Contains(profession);
                yield return hasProfession.ToString();
            }
            else
            {
                foreach (Profession profession in this.Professions)
                    yield return profession.ToString();
            }
        }

        /// <summary>Validate that the provided value is valid for an input argument (regardless of whether they match).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="error">The validation error, if any.</param>
        /// <returns>Returns whether validation succeeded.</returns>
        protected override bool TryValidate(string input, string value, out string error)
        {
            if (!base.TryValidate(input, value, out error))
                return false;

            // validate profession IDs
            string profession = input ?? value;
            if (!this.TryParseEnum(profession, out Profession _, mustBeNamed: false))
            {
                error = $"can't parse '{profession}' as a profession ID; must be one of [{string.Join(", ", Enum.GetNames(typeof(Profession)).OrderByIgnoreCase(p => p))}] or an integer ID.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
