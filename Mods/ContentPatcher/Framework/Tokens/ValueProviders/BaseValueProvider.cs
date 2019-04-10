using System;
using System.Collections.Generic;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders
{
    /// <summary>The base class for a value provider.</summary>
    internal abstract class BaseValueProvider : IValueProvider
    {
        /*********
        ** Fields
        *********/
        /// <summary>Whether multiple values may exist when no input is provided.</summary>
        protected bool CanHaveMultipleValuesForRoot { get; set; }

        /// <summary>Whether multiple values may exist when an input argument is provided.</summary>
        protected bool CanHaveMultipleValuesForInput { get; set; }


        /*********
        ** Accessors
        *********/
        /// <summary>The value provider name.</summary>
        public string Name { get; }

        /// <summary>Whether the provided values can change after the provider is initialised.</summary>
        public bool IsMutable { get; protected set; } = true;

        /// <summary>Whether the value provider allows an input argument (e.g. an NPC name for a relationship token).</summary>
        public bool AllowsInput { get; private set; }

        /// <summary>Whether the value provider requires an input argument to work, and does not provide values without it (see <see cref="IValueProvider.AllowsInput"/>).</summary>
        public bool RequiresInput { get; private set; }

        /// <summary>Whether values exist in the current context.</summary>
        public bool IsValidInContext { get; protected set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Update the underlying values.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the values changed.</returns>
        public virtual void UpdateContext(IContext context) { }

        /// <summary>Whether the value provider may return multiple values for the given input.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        public bool CanHaveMultipleValues(string input = null)
        {
            return input != null
                ? this.CanHaveMultipleValuesForInput
                : this.CanHaveMultipleValuesForRoot;
        }

        /// <summary>Validate that the provided values are valid for the input argument (regardless of whether they match).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <param name="values">The values to validate.</param>
        /// <param name="error">The validation error, if any.</param>
        /// <returns>Returns whether validation succeeded.</returns>
        public bool TryValidate(string input, InvariantHashSet values, out string error)
        {
            // parse data
            KeyValuePair<string, string>[] pairs = this.GetInputValuePairs(input, values).ToArray();

            // restrict to allowed input
            if (this.AllowsInput)
            {
                InvariantHashSet validInputs = this.GetValidInputs();
                if (validInputs?.Any() == true)
                {
                    string[] invalidInputs =
                        (
                            from pair in pairs
                            where pair.Key != null && !validInputs.Contains(pair.Key)
                            select pair.Key
                        )
                        .Distinct()
                        .ToArray();
                    if (invalidInputs.Any())
                    {
                        error = $"invalid input arguments ({string.Join(", ", invalidInputs)}), expected any of {string.Join(", ", validInputs)}";
                        return false;
                    }
                }
            }

            // restrict to allowed values
            {
                InvariantHashSet validValues = this.GetAllowedValues(input);
                if (validValues?.Any() == true)
                {
                    string[] invalidValues =
                        (
                            from pair in pairs
                            where !validValues.Contains(pair.Value)
                            select pair.Value
                        )
                        .Distinct()
                        .ToArray();
                    if (invalidValues.Any())
                    {
                        error = $"invalid values ({string.Join(", ", invalidValues)}); expected one of {string.Join(", ", validValues)}";
                        return false;
                    }
                }
            }

            // custom validation
            foreach (KeyValuePair<string, string> pair in pairs)
            {
                if (!this.TryValidate(pair.Key, pair.Value, out error))
                    return false;
            }

            // no issues found
            error = null;
            return true;
        }

        /// <summary>Get the set of valid input arguments if restricted, or an empty collection if unrestricted.</summary>
        public virtual InvariantHashSet GetValidInputs()
        {
            return new InvariantHashSet();
        }

        /// <summary>Get the allowed values for an input argument (or <c>null</c> if any value is allowed).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public virtual InvariantHashSet GetAllowedValues(string input)
        {
            return null;
        }

        /// <summary>Get the current values.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="IValueProvider.AllowsInput"/> or <see cref="IValueProvider.RequiresInput"/>.</exception>
        public virtual IEnumerable<string> GetValues(string input)
        {
            this.AssertInputArgument(input);
            yield break;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">The value provider name.</param>
        /// <param name="canHaveMultipleValuesForRoot">Whether the root value provider may contain multiple values.</param>
        protected BaseValueProvider(string name, bool canHaveMultipleValuesForRoot)
        {
            this.Name = name;
            this.CanHaveMultipleValuesForRoot = canHaveMultipleValuesForRoot;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="type">The value provider name.</param>
        /// <param name="canHaveMultipleValuesForRoot">Whether the root value provider may contain multiple values.</param>
        protected BaseValueProvider(ConditionType type, bool canHaveMultipleValuesForRoot)
            : this(type.ToString(), canHaveMultipleValuesForRoot) { }

        /// <summary>Validate that the provided value is valid for an input argument (regardless of whether they match).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="error">The validation error, if any.</param>
        /// <returns>Returns whether validation succeeded.</returns>
        protected virtual bool TryValidate(string input, string value, out string error)
        {
            error = null;
            return true;
        }

        /// <summary>Enable input arguments for this value provider.</summary>
        /// <param name="required">Whether an input argument is required when using this value provider.</param>
        /// <param name="canHaveMultipleValues">Whether the value provider may return multiple values for an input argument.</param>
        protected void EnableInputArguments(bool required, bool canHaveMultipleValues)
        {
            this.AllowsInput = true;
            this.RequiresInput = required;
            this.CanHaveMultipleValuesForInput = canHaveMultipleValues;
        }

        /// <summary>Assert that an input argument is valid for the value provider.</summary>
        /// <param name="input">The input argument to check, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="AllowsInput"/> or <see cref="RequiresInput"/>.</exception>
        protected void AssertInputArgument(string input)
        {
            if (input == null)
            {
                // missing input argument
                if (this.RequiresInput)
                    throw new InvalidOperationException($"The '{this.Name}' token requires an input argument.");
            }
            else
            {
                // no subkey allowed
                if (!this.AllowsInput)
                    throw new InvalidOperationException($"The '{this.Name}' token does not allow input arguments.");
            }
        }

        /// <summary>Try to parse a raw case-insensitive string into an enum value.</summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="raw">The raw string to parse.</param>
        /// <param name="result">The resulting enum value.</param>
        /// <param name="mustBeNamed">When parsing a numeric value, whether it must match one of the named enum values.</param>
        protected bool TryParseEnum<TEnum>(string raw, out TEnum result, bool mustBeNamed = true) where TEnum : struct
        {
            if (!Enum.TryParse(raw, true, out result))
                return false;

            if (mustBeNamed && !Enum.IsDefined(typeof(TEnum), result))
                return false;

            return true;
        }

        /// <summary>Parse a user-defined set of values for input/value pairs. For example, <c>"Abigail:10"</c> for a relationship token would be parsed as input argument 'Abigail' with value '10'.</summary>
        /// <param name="input">The current input argument, if applicable.</param>
        /// <param name="values">The values to parse.</param>
        /// <returns>Returns the input/value pairs found. If <paramref name="input"/> is non-null, the <paramref name="values"/> are treated as values for that input argument. Otherwise if <see cref="AllowsInput"/> is true, then each value is treated as <c>input:value</c> (if they contain a colon) or <c>value</c> (with a null input).</returns>
        protected IEnumerable<KeyValuePair<string, string>> GetInputValuePairs(string input, InvariantHashSet values)
        {
            // no input arguments in values
            if (!this.AllowsInput || input != null)
            {
                foreach (string value in values)
                    yield return new KeyValuePair<string, string>(input, value);
            }

            // possible input arguments in values
            else
            {
                foreach (string value in values)
                {
                    string[] parts = value.Split(new[] { ':' }, 2);
                    if (parts.Length < 2)
                        yield return new KeyValuePair<string, string>(input, parts[0]);
                    else
                        yield return new KeyValuePair<string, string>(parts[0], parts[1]);
                }
            }
        }
    }
}
