namespace ContentPatcher.Framework.Lexing.LexTokens
{
    /// <summary>A lexical character pattern type.</summary>
    public enum LexBitType
    {
        /// <summary>A literal string.</summary>
        Literal,

        /// <summary>The characters which start a token ('{{').</summary>
        StartToken,

        /// <summary>The characters which end a token ('}}').</summary>
        EndToken,

        /// <summary>The character which separates a token name from its input argument (':').</summary>
        InputArgSeparator,

        /// <summary>The character which pipes the output of one token into the input of another ('|').</summary>
        TokenPipe
    }
}
