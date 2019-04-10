using System.Text;

namespace ContentPatcher.Framework.Lexing.LexTokens
{
    /// <summary>A lexical token representing a Content Patcher token.</summary>
    internal readonly struct LexTokenToken : ILexToken
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The lexical token type.</summary>
        public LexTokenType Type { get; }

        /// <summary>A text representation of the lexical token.</summary>
        public string Text { get; }

        /// <summary>The Content Patcher token name.</summary>
        public string Name { get; }

        /// <summary>The input argument passed to the Content Patcher token.</summary>
        public LexTokenInputArg? InputArg { get; }

        /// <summary>Whether the token omits the start/end character patterns because it's in a token-only context.</summary>
        public bool ImpliedBraces { get; }

        /// <summary>A sequence of tokens to invoke after this token is processed, each getting the output of the previous token as its input.</summary>
        public LexTokenToken[] PipedTokens { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">The Content Patcher token name.</param>
        /// <param name="inputArg">The input argument passed to the Content Patcher token.</param>
        /// <param name="impliedBraces">Whether the token omits the start/end character patterns because it's in a token-only context.</param>
        /// <param name="pipedTokens">A sequence of tokens to invoke after this token is processed, each getting the output of the previous token as its input.</param>
        public LexTokenToken(string name, LexTokenInputArg? inputArg, bool impliedBraces, LexTokenToken[] pipedTokens)
        {
            this.Type = LexTokenType.Token;
            this.Text = LexTokenToken.GetRawText(name, inputArg, impliedBraces);
            this.Name = name;
            this.InputArg = inputArg;
            this.ImpliedBraces = impliedBraces;
            this.PipedTokens = pipedTokens;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a string representation of a token.</summary>
        /// <param name="name">The Content Patcher token name.</param>
        /// <param name="tokenInputArgArgument">The input argument passed to the Content Patcher token.</param>
        /// <param name="impliedBraces">Whether the token omits the start/end character patterns because it's in a token-only context.</param>
        private static string GetRawText(string name, LexTokenInputArg? tokenInputArgArgument, bool impliedBraces)
        {
            StringBuilder str = new StringBuilder();
            if (!impliedBraces)
                str.Append("{{");
            str.Append(name);
            if (tokenInputArgArgument != null)
            {
                str.Append(":");
                str.Append(tokenInputArgArgument.Value.Text);
            }
            if (!impliedBraces)
                str.Append("}}");
            return str.ToString();
        }
    }
}
