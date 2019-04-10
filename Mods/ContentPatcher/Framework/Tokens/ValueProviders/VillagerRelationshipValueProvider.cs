using System;
using System.Collections.Generic;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common.Utilities;
using StardewModdingAPI;
using StardewValley;

namespace ContentPatcher.Framework.Tokens.ValueProviders
{
    /// <summary>A value provider for NPC relationship types.</summary>
    internal class VillagerRelationshipValueProvider : BaseValueProvider
    {
        /*********
        ** Fields
        *********/
        /// <summary>The relationships by NPC.</summary>
        private readonly InvariantDictionary<string> Values = new InvariantDictionary<string>();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public VillagerRelationshipValueProvider()
            : base(ConditionType.Relationship, canHaveMultipleValuesForRoot: false)
        {
            this.EnableInputArguments(required: false, canHaveMultipleValues: false);
        }

        /// <summary>Update the underlying values.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the values changed.</returns>
        public override void UpdateContext(IContext context)
        {
            this.Values.Clear();
            this.IsValidInContext = Context.IsWorldReady;
            if (this.IsValidInContext)
            {
                foreach (KeyValuePair<string, Friendship> pair in Game1.player.friendshipData.Pairs)
                    this.Values[pair.Key] = pair.Value.Status.ToString();
            }
        }

        /// <summary>Get the set of valid input arguments if restricted, or an empty collection if unrestricted.</summary>
        public override InvariantHashSet GetValidInputs()
        {
            return new InvariantHashSet(this.Values.Keys);
        }

        /// <summary>Get the current values.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override IEnumerable<string> GetValues(string input)
        {
            this.AssertInputArgument(input);

            if (input != null)
            {
                if (this.Values.TryGetValue(input, out string value))
                    yield return value;
            }
            else
            {
                foreach (var pair in this.Values)
                    yield return $"{pair.Key}:{pair.Value}";
            }
        }
    }
}
