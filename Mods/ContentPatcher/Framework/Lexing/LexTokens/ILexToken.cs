namespace ContentPatcher.Framework.Lexing.LexTokens
{
    /// <summary>A lexical token within a string, which combines one or more <see cref="LexBit"/> patterns into a cohesive part.</summary>
    internal interface ILexToken
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The lexical token type.</summary>
        LexTokenType Type { get; }

        /// <summary>A text representation of the lexical token.</summary>
        string Text { get; }
    }
}
