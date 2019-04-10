using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContentPatcher.Framework.Lexing;
using ContentPatcher.Framework.Lexing.LexTokens;
using ContentPatcher.Framework.Tokens;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Conditions
{
    /// <summary>A string value which can contain condition tokens.</summary>
    internal class TokenString
    {
        /*********
        ** Fields
        *********/
        /// <summary>The lexical tokens parsed from the raw string.</summary>
        private readonly ILexToken[] LexTokens;

        /// <summary>The underlying value for <see cref="Value"/>.</summary>
        private string ValueImpl;

        /// <summary>The underlying value for <see cref="IsReady"/>.</summary>
        private bool IsReadyImpl;


        /*********
        ** Accessors
        *********/
        /// <summary>The raw string without token substitution.</summary>
        public string Raw { get; }

        /// <summary>The tokens used in the string.</summary>
        public HashSet<TokenName> Tokens { get; } = new HashSet<TokenName>();

        /// <summary>The unrecognised tokens in the string.</summary>
        public InvariantHashSet InvalidTokens { get; } = new InvariantHashSet();

        /// <summary>Whether the string contains any tokens (including invalid tokens).</summary>
        public bool HasAnyTokens => this.Tokens.Count > 0 || this.InvalidTokens.Count > 0;

        /// <summary>Whether the token string value may change depending on the context.</summary>
        public bool IsMutable { get; }

        /// <summary>Whether the token string consists of a single token with no surrounding text.</summary>
        public bool IsSingleTokenOnly { get; }

        /// <summary>The string with tokens substituted for the last context update.</summary>
        public string Value => this.ValueImpl;

        /// <summary>Whether all tokens in the value have been replaced.</summary>
        public bool IsReady => this.IsReadyImpl;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="raw">The raw string before token substitution.</param>
        /// <param name="tokenContext">The available token context.</param>
        public TokenString(string raw, IContext tokenContext)
        {
            // set raw value
            this.Raw = raw?.Trim();
            if (string.IsNullOrWhiteSpace(this.Raw))
            {
                this.ValueImpl = this.Raw;
                this.IsReadyImpl = true;
                return;
            }

            // extract tokens
            this.LexTokens = new Lexer().ParseBits(raw, impliedBraces: false).ToArray();
            foreach (LexTokenToken token in this.LexTokens.OfType<LexTokenToken>())
            {
                TokenName name = new TokenName(token.Name, token.InputArg?.Text);
                if (tokenContext.Contains(name, enforceContext: false))
                    this.Tokens.Add(name);
                else
                    this.InvalidTokens.Add(token.Text);
            }

            // set metadata
            this.IsMutable = this.Tokens.Any();
            if (!this.IsMutable)
            {
                this.ValueImpl = this.Raw;
                this.IsReadyImpl = !this.InvalidTokens.Any();
            }
            this.IsSingleTokenOnly = this.LexTokens.Length == 1 && this.LexTokens.First().Type == LexTokenType.Token;
        }

        /// <summary>Update the <see cref="Value"/> with the given tokens.</summary>
        /// <param name="context">Provides access to contextual tokens.</param>
        /// <returns>Returns whether the value changed.</returns>
        public bool UpdateContext(IContext context)
        {
            if (!this.IsMutable)
                return false;

            string prevValue = this.Value;
            this.GetApplied(context, out this.ValueImpl, out this.IsReadyImpl);
            return this.Value != prevValue;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a new string with tokens substituted.</summary>
        /// <param name="context">Provides access to contextual tokens.</param>
        /// <param name="result">The input string with tokens substituted.</param>
        /// <param name="isReady">Whether all tokens in the <paramref name="result"/> have been replaced.</param>
        private void GetApplied(IContext context, out string result, out bool isReady)
        {
            bool allReplaced = true;
            StringBuilder str = new StringBuilder();
            foreach (ILexToken lexToken in this.LexTokens)
            {
                switch (lexToken)
                {
                    case LexTokenToken lexTokenToken:
                        TokenName name = new TokenName(lexTokenToken.Name, lexTokenToken.InputArg?.Text);
                        IToken token = context.GetToken(name, enforceContext: true);
                        if (token != null)
                            str.Append(token.GetValues(name).FirstOrDefault());
                        else
                        {
                            allReplaced = false;
                            str.Append(lexToken.Text);
                        }
                        break;

                    default:
                        str.Append(lexToken.Text);
                        break;
                }
            }

            result = str.ToString();
            isReady = allReplaced;
        }
    }
}
