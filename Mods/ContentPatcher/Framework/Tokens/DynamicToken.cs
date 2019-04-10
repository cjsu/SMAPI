using ContentPatcher.Framework.Tokens.ValueProviders;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens
{
    /// <summary>A dynamic token defined by a content pack.</summary>
    internal class DynamicToken : GenericToken
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying value provider.</summary>
        private readonly DynamicTokenValueProvider DynamicValues;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">The token name.</param>
        public DynamicToken(TokenName name)
            : base(new DynamicTokenValueProvider(name.Key))
        {
            this.DynamicValues = (DynamicTokenValueProvider)base.Values;
        }

        /// <summary>Add a set of possible values.</summary>
        /// <param name="possibleValues">The possible values to add.</param>
        public void AddAllowedValues(InvariantHashSet possibleValues)
        {
            this.DynamicValues.AddAllowedValues(possibleValues);
            this.CanHaveMultipleRootValues = this.DynamicValues.CanHaveMultipleValues();
        }

        /// <summary>Set the current values.</summary>
        /// <param name="values">The values to set.</param>
        public void SetValue(InvariantHashSet values)
        {
            this.DynamicValues.SetValue(values);
        }

        /// <summary>Set whether the token is valid in the current context.</summary>
        /// <param name="validInContext">The value to set.</param>
        public void SetValidInContext(bool validInContext)
        {
            this.DynamicValues.SetValidInContext(validInContext);
            this.IsValidInContext = this.DynamicValues.IsValidInContext;
        }
    }
}
