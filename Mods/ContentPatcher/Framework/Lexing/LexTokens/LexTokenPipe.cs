namespace ContentPatcher.Framework.Lexing.LexTokens
{
    /// <summary>A lexical token which represents a pipe that transfers the output of one token into the input of another.</summary>
    internal readonly struct LexTokenPipe : ILexToken
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
        /// <param name="text">A text representation of the lexical token.</param>
        public LexTokenPipe(string text)
        {
            this.Type = LexTokenType.TokenPipe;
            this.Text = text;
        }
    }
}
