using System;
using System.Collections.Generic;

namespace ContentPatcher.Framework.Tokens
{
    /// <summary>Provides access to contextual tokens.</summary>
    internal interface IContext
    {
        /// <summary>Get whether the context contains the given token.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        bool Contains(TokenName name, bool enforceContext);

        /// <summary>Get the underlying token which handles a key.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        /// <returns>Returns the matching token, or <c>null</c> if none was found.</returns>
        IToken GetToken(TokenName name, bool enforceContext);

        /// <summary>Get the underlying tokens.</summary>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        IEnumerable<IToken> GetTokens(bool enforceContext);

        /// <summary>Get the current values of the given token for comparison.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        /// <returns>Return the values of the matching token, or an empty list if the token doesn't exist.</returns>
        /// <exception cref="ArgumentNullException">The specified key is null.</exception>
        IEnumerable<string> GetValues(TokenName name, bool enforceContext);
    }
}
