using System;
using System.Collections.Generic;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders
{
    /// <summary>A value provider for a built-in condition whose value may change with the context.</summary>
    internal class ConditionTypeValueProvider : BaseValueProvider
    {
        /*********
        ** Fields
        *********/
        /// <summary>The allowed root values (or <c>null</c> if any value is allowed).</summary>
        private readonly InvariantHashSet AllowedRootValues;

        /// <summary>Get the current values.</summary>
        private readonly Func<InvariantHashSet> FetchValues;

        /// <summary>Get whether the value provider is applicable in the current context, or <c>null</c> if it's always applicable.</summary>
        private readonly Func<bool> IsValidInContextImpl;

        /// <summary>The values as of the last context update.</summary>
        private readonly InvariantHashSet Values = new InvariantHashSet();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The condition type.</param>
        /// <param name="values">Get the current values.</param>
        /// <param name="isValidInContext">Get whether the value provider is applicable in the current context, or <c>null</c> if it's always applicable.</param>
        /// <param name="canHaveMultipleValues">Whether the root may contain multiple values.</param>
        /// <param name="allowedValues">The allowed values (or <c>null</c> if any value is allowed).</param>
        public ConditionTypeValueProvider(ConditionType type, Func<IEnumerable<string>> values, Func<bool> isValidInContext = null, bool canHaveMultipleValues = false, IEnumerable<string> allowedValues = null)
            : base(type, canHaveMultipleValues)
        {
            this.IsValidInContextImpl = isValidInContext;
            this.AllowedRootValues = allowedValues != null ? new InvariantHashSet(allowedValues) : null;
            this.FetchValues = () => new InvariantHashSet(values());
            this.EnableInputArguments(required: false, canHaveMultipleValues: false);
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="type">The condition type.</param>
        /// <param name="value">Get the current value.</param>
        /// <param name="isValidInContext">Get whether the value provider is applicable in the current context, or <c>null</c> if it's always applicable.</param>
        /// <param name="canHaveMultipleValues">Whether the root may contain multiple values.</param>
        /// <param name="allowedValues">The allowed values (or <c>null</c> if any value is allowed).</param>
        public ConditionTypeValueProvider(ConditionType type, Func<string> value, Func<bool> isValidInContext = null, bool canHaveMultipleValues = false, IEnumerable<string> allowedValues = null)
            : this(type, () => new[] { value() }, isValidInContext, canHaveMultipleValues, allowedValues) { }

        /// <summary>Update the underlying values.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the values changed.</returns>
        public override void UpdateContext(IContext context)
        {
            this.IsValidInContext = this.IsValidInContextImpl == null || this.IsValidInContextImpl();
            this.Values.Clear();
            if (this.IsValidInContext)
            {
                foreach (string value in this.FetchValues())
                    this.Values.Add(value);
            }
        }

        /// <summary>Get the allowed values for an input argument (or <c>null</c> if any value is allowed).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override InvariantHashSet GetAllowedValues(string input)
        {
            return input != null
                ? InvariantHashSet.Boolean()
                : this.AllowedRootValues;
        }

        /// <summary>Get the current values.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public override IEnumerable<string> GetValues(string input)
        {
            this.AssertInputArgument(input);

            if (input != null)
                return new[] { this.Values.Contains(input).ToString() };
            return this.Values;
        }
    }
}
