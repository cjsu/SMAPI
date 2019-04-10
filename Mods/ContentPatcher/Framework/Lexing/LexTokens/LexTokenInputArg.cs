using System.Linq;

namespace ContentPatcher.Framework.Lexing.LexTokens
{
    /// <summary>A lexical token representing the input argument for a Content Patcher token.</summary>
    internal readonly struct LexTokenInputArg : ILexToken
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The lexical token type.</summary>
        public LexTokenType Type { get; }

        /// <summary>A text representation of the lexical token.</summary>
        public string Text { get; }

        /// <summary>The lexical tokens making up the input argument.</summary>
        public ILexToken[] Parts { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="tokenParts">The lexical tokens making up the input argument.</param>
        public LexTokenInputArg(ILexToken[] tokenParts)
        {
            this.Type = LexTokenType.TokenInput;
            this.Text = string.Join("", tokenParts.Select(p => p.Text));
            this.Parts = tokenParts;
        }
    }
}
