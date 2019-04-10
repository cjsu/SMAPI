namespace ContentPatcher.Framework.Lexing.LexTokens
{
    /// <summary>A lexical token representing a literal string value.</summary>
    internal readonly struct LexTokenLiteral : ILexToken
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The lexical token type.</summary>
        public LexTokenType Type { get; }

        /// <summary>A text representation of the lexical token.</summary>
        public string Text { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="text">The literal text value.</param>
        public LexTokenLiteral(string text)
        {
            this.Type = LexTokenType.Literal;
            this.Text = text;
        }
    }
}
