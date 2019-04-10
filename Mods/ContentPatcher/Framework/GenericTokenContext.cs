using System;
using System.Collections.Generic;
using ContentPatcher.Framework.Tokens;

namespace ContentPatcher.Framework
{
    /// <summary>A generic token context.</summary>
    /// <typeparam name="TToken">The token type to store.</typeparam>
    internal class GenericTokenContext<TToken> : IContext where TToken : class, IToken
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The available tokens.</summary>
        public IDictionary<TokenName, TToken> Tokens { get; } = new Dictionary<TokenName, TToken>();


        /*********
        ** Accessors
        *********/
        /// <summary>Save the given token to the context.</summary>
        /// <param name="token">The token to save.</param>
        public void Save(TToken token)
        {
            this.Tokens[token.Name] = token;
        }

        /// <summary>Get whether the context contains the given token.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        public bool Contains(TokenName name, bool enforceContext)
        {
            return this.GetToken(name, enforceContext) != null;
        }

        /// <summary>Get the underlying token which handles a key.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        /// <returns>Returns the matching token, or <c>null</c> if none was found.</returns>
        public IToken GetToken(TokenName name, bool enforceContext)
        {
            return this.Tokens.TryGetValue(name.GetRoot(), out TToken token) && this.ShouldConsider(token, enforceContext)
                ? token
                : null;
        }

        /// <summary>Get the underlying tokens.</summary>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        public IEnumerable<IToken> GetTokens(bool enforceContext)
        {
            foreach (TToken token in this.Tokens.Values)
            {
                if (this.ShouldConsider(token, enforceContext))
                    yield return token;
            }
        }

        /// <summary>Get the current values of the given token for comparison.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        /// <returns>Return the values of the matching token, or an empty list if the token doesn't exist.</returns>
        /// <exception cref="ArgumentNullException">The specified key is null.</exception>
        public IEnumerable<string> GetValues(TokenName name, bool enforceContext)
        {
            IToken token = this.GetToken(name, enforceContext);
            return token?.GetValues(name) ?? new string[0];
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether a given token should be considered.</summary>
        /// <param name="token">The token to check.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        private bool ShouldConsider(IToken token, bool enforceContext)
        {
            return !enforceContext || token.IsValidInContext;
        }
    }

    /// <summary>A generic token context.</summary>
    internal class GenericTokenContext : GenericTokenContext<IToken> { }
}
