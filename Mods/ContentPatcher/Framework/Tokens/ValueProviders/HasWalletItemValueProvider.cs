using System;
using System.Collections.Generic;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.Constants;
using Pathoschild.Stardew.Common.Utilities;
using StardewValley;

namespace ContentPatcher.Framework.Tokens.ValueProviders
{
    /// <summary>A value provider for the player's wallet items.</summary>
    internal class HasWalletItemValueProvider : BaseValueProvider
    {
        /*********
        ** Fields
        *********/
        /// <summary>Get whether the player data is available in the current context.</summary>
        private readonly Func<bool> IsPlayerDataAvailable;

        /// <summary>The defined wallet items and whether the player has them.</summary>
        private readonly IDictionary<WalletItem, Func<bool>> WalletItems = new Dictionary<WalletItem, Func<bool>>
        {
            [WalletItem.DwarvishTranslationGuide] = () => Game1.player.canUnderstandDwarves,
            [WalletItem.RustyKey] = () => Game1.player.hasRustyKey,
            [WalletItem.ClubCard] = () => Game1.player.hasClubCard,
            [WalletItem.SpecialCharm] = () => Game1.player.hasSpecialCharm,
            [WalletItem.SkullKey] = () => Game1.player.hasSkullKey,
            [WalletItem.MagnifyingGlass] = () => Game1.player.hasMagnifyingGlass,
            [WalletItem.DarkTalisman] = () => Game1.player.hasDarkTalisman,
            [WalletItem.MagicInk] = () => Game1.player.hasMagicInk,
            [WalletItem.BearsKnowledge] = () => Game1.player.eventsSeen.Contains(2120303),
            [WalletItem.SpringOnionMastery] = () => Game1.player.eventsSeen.Contains(3910979)
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="isPlayerDataAvailable">Get whether the player data is available in the current context.</param>
        public HasWalletItemValueProvider(Func<bool> isPlayerDataAvailable)
            : base(ConditionType.HasWalletItem, canHaveMultipleValuesForRoot: true)
        {
            this.IsPlayerDataAvailable = isPlayerDataAvailable;
            this.EnableInputArguments(required: false, canHaveMultipleValues: false);
        }

        /// <summary>Update the underlying values.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the values changed.</returns>
        public override void UpdateContext(IContext context)
        {
            this.IsValidInContext = this.IsPlayerDataAvailable();
        }

        /// <summary>Get the set of valid input arguments if restricted, or an empty collection if unrestricted.</summary>
        public override InvariantHashSet GetValidInputs()
        {
            return new InvariantHashSet(this.WalletItems.Keys.Select(p => p.ToString()));
        }

        /// <summary>Get the allowed values for an input argument (or <c>null</c> if any value is allowed).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override InvariantHashSet GetAllowedValues(string input)
        {
            return input != null
                ? InvariantHashSet.Boolean()
                : this.GetValidInputs();
        }

        /// <summary>Get the current values.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override IEnumerable<string> GetValues(string input)
        {
            this.AssertInputArgument(input);

            if (input != null)
            {
                bool hasItem = this.TryParseEnum(input, out WalletItem item) && this.WalletItems[item]();
                yield return hasItem.ToString();
            }
            else
            {
                foreach (KeyValuePair<WalletItem, Func<bool>> pair in this.WalletItems)
                {
                    if (pair.Value())
                        yield return pair.Key.ToString();
                }
            }
        }
    }
}
