using System;
using System.Collections.Generic;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Tokens
{
    /// <summary>A token whose value may change depending on the current context.</summary>
    internal interface IToken
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The token name.</summary>
        TokenName Name { get; }

        /// <summary>Whether the token is applicable in the current context.</summary>
        bool IsValidInContext { get; }

        /// <summary>Whether the value can change after it's initialised.</summary>
        bool IsMutable { get; }

        /// <summary>Whether this token recognises subkeys (e.g. <c>Relationship:Abigail</c> is a <c>Relationship</c> token with a <c>Abigail</c> subkey).</summary>
        bool CanHaveSubkeys { get; }

        /// <summary>Whether this token only allows subkeys (see <see cref="CanHaveSubkeys"/>).</summary>
        bool RequiresSubkeys { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Update the token data when the context changes.</summary>
        /// <param name="context">The condition context.</param>
        /// <returns>Returns whether the token data changed.</returns>
        void UpdateContext(IContext context);

        /// <summary>Whether the token may return multiple values for the given name.</summary>
        /// <param name="name">The token name.</param>
        bool CanHaveMultipleValues(TokenName name);

        /// <summary>Perform custom validation.</summary>
        /// <param name="name">The token name to validate.</param>
        /// <param name="values">The values to validate.</param>
        /// <param name="error">The validation error, if any.</param>
        /// <returns>Returns whether validation succeeded.</returns>
        bool TryValidate(TokenName name, InvariantHashSet values, out string error);

        /// <summary>Get the current subkeys (if supported).</summary>
        IEnumerable<TokenName> GetSubkeys();

        /// <summary>Get the allowed values for a token name (or <c>null</c> if any value is allowed).</summary>
        /// <exception cref="InvalidOperationException">The key doesn't match this token, or the key does not respect <see cref="CanHaveSubkeys"/> or <see cref="RequiresSubkeys"/>.</exception>
        InvariantHashSet GetAllowedValues(TokenName name);

        /// <summary>Get the current token values.</summary>
        /// <param name="name">The token name to check.</param>
        /// <exception cref="InvalidOperationException">The key doesn't match this token, or the key does not respect <see cref="CanHaveSubkeys"/> or <see cref="RequiresSubkeys"/>.</exception>
        IEnumerable<string> GetValues(TokenName name);
    }
}
