using System;
using System.Collections.Generic;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens.ValueProviders
{
    /// <summary>Provides values for a token name with optional input.</summary>
    internal interface IValueProvider
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The value provider name.</summary>
        string Name { get; }

        /// <summary>Whether values exist in the current context.</summary>
        bool IsValidInContext { get; }

        /// <summary>Whether the provided values can change after the provider is initialised.</summary>
        bool IsMutable { get; }

        /// <summary>Whether the value provider allows an input argument (e.g. an NPC name for a relationship token).</summary>
        bool AllowsInput { get; }

        /// <summary>Whether the value provider requires an input argument to work, and does not provide values without it (see <see cref="AllowsInput"/>).</summary>
        bool RequiresInput { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Update the underlying values.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the values changed.</returns>
        void UpdateContext(IContext context);

        /// <summary>Whether the value provider may return multiple values for the given input.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        bool CanHaveMultipleValues(string input = null);

        /// <summary>Validate that the provided values are valid for the input argument (regardless of whether they match).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <param name="values">The values to validate.</param>
        /// <param name="error">The validation error, if any.</param>
        /// <returns>Returns whether validation succeeded.</returns>
        bool TryValidate(string input, InvariantHashSet values, out string error);

        /// <summary>Get the set of valid input arguments if restricted, or an empty collection if unrestricted.</summary>
        InvariantHashSet GetValidInputs();

        /// <summary>Get the allowed values for an input argument (or <c>null</c> if any value is allowed).</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="AllowsInput"/> or <see cref="RequiresInput"/>.</exception>
        InvariantHashSet GetAllowedValues(string input);

        /// <summary>Get the current values.</summary>
        /// <param name="input">The input argument, if applicable.</param>
        /// <exception cref="InvalidOperationException">The input argument doesn't match this value provider, or does not respect <see cref="AllowsInput"/> or <see cref="RequiresInput"/>.</exception>
        IEnumerable<string> GetValues(string input);
    }
}
