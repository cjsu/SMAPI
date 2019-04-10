using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ContentPatcher.Framework.Lexing.LexTokens;

namespace ContentPatcher.Framework.Lexing
{
    /// <summary>Handles parsing raw strings into tokens.</summary>
    internal class Lexer
    {
        /*********
        ** Fields
        *********/
        /// <summary>A regular expression which matches lexical patterns that split lexical patterns. For example, ':' is a <see cref="LexBitType.InputArgSeparator"/> pattern that splits a token name and its input arguments. The split pattern is itself a lexical pattern.</summary>
        private readonly Regex LexicalSplitPattern = new Regex(@"({{|}}|:|\|)", RegexOptions.Compiled);


        /*********
        ** Public methods
        *********/
        /// <summary>Break a raw string into its constituent lexical character patterns.</summary>
        /// <param name="rawText">The raw text to tokenise.</param>
        public IEnumerable<LexBit> TokeniseString(string rawText)
        {
            // special cases
            if (rawText == null)
                yield break;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                yield return new LexBit(LexBitType.Literal, rawText);
                yield break;
            }

            // parse
            string[] parts = this.LexicalSplitPattern.Split(rawText);
            foreach (string part in parts)
            {
                if (part == "")
                    continue; // split artifact

                LexBitType type;
                switch (part)
                {
                    case "{{":
                        type = LexBitType.StartToken;
                        break;

                    case "}}":
                        type = LexBitType.EndToken;
                        break;

                    case ":":
                        type = LexBitType.InputArgSeparator;
                        break;

                    case "|":
                        type = LexBitType.TokenPipe;
                        break;

                    default:
                        type = LexBitType.Literal;
                        break;
                }

                yield return new LexBit(type, part);
            }
        }

        /// <summary>Parse a sequence of lexical character patterns into higher-level lexical tokens.</summary>
        /// <param name="rawText">The raw text to tokenise.</param>
        /// <param name="impliedBraces">Whether we're parsing a token context (so the outer '{{' and '}}' are implied); else parse as a tokenisable string which main contain a mix of literal and {{token}} values.</param>
        public IEnumerable<ILexToken> ParseBits(string rawText, bool impliedBraces)
        {
            IEnumerable<LexBit> bits = this.TokeniseString(rawText);
            return this.ParseBits(bits, impliedBraces);
        }

        /// <summary>Parse a sequence of lexical character patterns into higher-level lexical tokens.</summary>
        /// <param name="bits">The lexical character patterns to parse.</param>
        /// <param name="impliedBraces">Whether we're parsing a token context (so the outer '{{' and '}}' are implied); else parse as a tokenisable string which main contain a mix of literal and {{token}} values.</param>
        public IEnumerable<ILexToken> ParseBits(IEnumerable<LexBit> bits, bool impliedBraces)
        {
            return this.ParseBitQueue(new Queue<LexBit>(bits), impliedBraces, trim: false);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Parse a sequence of lexical character patterns into higher-level lexical tokens.</summary>
        /// <param name="input">The lexical character patterns to parse.</param>
        /// <param name="impliedBraces">Whether we're parsing a token context (so the outer '{{' and '}}' are implied); else parse as a tokenisable string which main contain a mix of literal and {{token}} values.</param>
        /// <param name="trim">Whether the value should be trimmed.</param>
        private IEnumerable<ILexToken> ParseBitQueue(Queue<LexBit> input, bool impliedBraces, bool trim)
        {
            // perform a raw parse
            IEnumerable<ILexToken> RawParse()
            {
                // 'Implied braces' means we're parsing inside a token. This necessarily starts with a token name,
                // optionally followed by an input argument and token pipes.
                if (impliedBraces)
                {
                    while (input.Any())
                    {
                        yield return this.ExtractToken(input, impliedBraces: true);
                        if (!input.Any())
                            yield break;

                        var next = input.Peek();
                        switch (next.Type)
                        {
                            case LexBitType.TokenPipe:
                                yield return new LexTokenPipe(input.Dequeue().Text);
                                break;

                            default:
                                throw new InvalidOperationException($"Unexpected {next.Type}, expected {LexBitType.Literal} or {LexBitType.TokenPipe}");
                        }
                    }
                    yield break;
                }

                // Otherwise this is a tokenisable string which may contain a mix of literal and {{token}} values.
                while (input.Any())
                {
                    LexBit next = input.Peek();
                    switch (next.Type)
                    {
                        // start token
                        case LexBitType.StartToken:
                            yield return this.ExtractToken(input, impliedBraces: false);
                            break;

                        // pipe/separator outside token
                        case LexBitType.Literal:
                        case LexBitType.TokenPipe:
                        case LexBitType.InputArgSeparator:
                            input.Dequeue();
                            yield return new LexTokenLiteral(next.Text);
                            break;

                        // anything else is invalid
                        default:
                            throw new InvalidOperationException($"Unexpected {next.Type}, expected {LexBitType.StartToken} or {LexBitType.Literal}");
                    }
                }
            }

            // normalise literal values
            LinkedList<ILexToken> tokens = new LinkedList<ILexToken>(RawParse());
            IList<LinkedListNode<ILexToken>> removeQueue = new List<LinkedListNode<ILexToken>>();
            for (LinkedListNode<ILexToken> node = tokens.First; node != null; node = node.Next)
            {
                if (node.Value.Type != LexTokenType.Literal)
                    continue;

                // fetch info
                ILexToken current = node.Value;
                ILexToken previous = node.Previous?.Value;
                ILexToken next = node.Next?.Value;
                string newText = node.Value.Text;

                // collapse sequential literals
                if (previous?.Type == LexTokenType.Literal)
                {
                    newText = previous.Text + newText;
                    removeQueue.Add(node.Previous);
                }

                // trim before/after separator
                if (next?.Type == LexTokenType.TokenInput || next?.Type == LexTokenType.TokenPipe)
                    newText = newText.TrimEnd();
                if (previous?.Type == LexTokenType.TokenInput || previous?.Type == LexTokenType.TokenPipe)
                    newText = newText.TrimStart();

                // trim whole result
                if (trim && (previous == null || next == null))
                {
                    if (previous == null)
                        newText = newText.TrimStart();
                    if (next == null)
                        newText = newText.TrimEnd();

                    if (newText == "")
                        removeQueue.Add(node);
                }

                // replace value if needed
                if (newText != current.Text)
                    node.Value = new LexTokenLiteral(newText);
            }
            foreach (LinkedListNode<ILexToken> entry in removeQueue)
                tokens.Remove(entry);

            // yield result
            return tokens;
        }

        /// <summary>Extract a token from the front of a lexical input queue.</summary>
        /// <param name="input">The input from which to extract a token. The extracted lexical bits will be removed from the queue.</param>
        /// <param name="impliedBraces">Whether we're parsing a token context (so the outer '{{' and '}}' are implied); else parse as a tokenisable string which main contain a mix of literal and {{token}} values.</param>
        /// <param name="endBeforePipe">Whether a <see cref="LexTokenType.TokenPipe"/> should signal the end of the token. Only valid if <see cref="impliedBraces"/> is true.</param>
        /// <returns>Returns the token, or multiple tokens if chained using <see cref="LexBitType.TokenPipe"/>.</returns>
        public LexTokenToken ExtractToken(Queue<LexBit> input, bool impliedBraces, bool endBeforePipe = false)
        {
            LexBit GetNextAndAssert()
            {
                if (!input.Any())
                    throw new InvalidOperationException();
                return input.Dequeue();
            }

            // start token
            if (!impliedBraces)
            {
                LexBit startToken = GetNextAndAssert();
                if (startToken.Type != LexBitType.StartToken)
                    throw new InvalidOperationException($"Unexpected {startToken.Type} at start of token.");
            }

            // extract token name
            LexBit name = GetNextAndAssert();
            if (name.Type != LexBitType.Literal)
                throw new InvalidOperationException($"Unexpected {name.Type} where token name should be.");

            // extract input argument if present
            LexTokenInputArg? inputArg = null;
            if (input.Any() && input.Peek().Type == LexBitType.InputArgSeparator)
            {
                input.Dequeue();
                inputArg = this.ExtractInputArgument(input);
            }

            // extract piped tokens
            IList<LexTokenToken> pipedTokens = new List<LexTokenToken>();
            if (!endBeforePipe)
            {
                while (input.Any() && input.Peek().Type == LexBitType.TokenPipe)
                {
                    input.Dequeue();
                    pipedTokens.Add(this.ExtractToken(input, impliedBraces: true, endBeforePipe: true));
                }
            }

            // end token
            if (!impliedBraces)
            {
                LexBit endToken = GetNextAndAssert();
                if (endToken.Type != LexBitType.EndToken)
                    throw new InvalidOperationException($"Unexpected {endToken.Type} before end of token.");
            }

            return new LexTokenToken(name.Text.Trim(), inputArg, impliedBraces, pipedTokens.ToArray());
        }

        /// <summary>Extract a token input argument from the front of a lexical input queue.</summary>
        /// <param name="input">The input from which to extract an input argument. The extracted lexical bits will be removed from the queue.</param>
        public LexTokenInputArg ExtractInputArgument(Queue<LexBit> input)
        {
            // extract input arg parts
            Queue<LexBit> inputArgBits = new Queue<LexBit>();
            int tokenDepth = 0;
            bool reachedEnd = false;
            while (!reachedEnd && input.Any())
            {
                LexBit next = input.Peek();
                switch (next.Type)
                {
                    case LexBitType.StartToken:
                        tokenDepth++;
                        inputArgBits.Enqueue(input.Dequeue());
                        break;

                    case LexBitType.TokenPipe:
                        if (tokenDepth > 0)
                            throw new InvalidOperationException($"Unexpected {next.Type} within token input argument");

                        reachedEnd = true;
                        break;

                    case LexBitType.EndToken:
                        tokenDepth--;

                        if (tokenDepth < 0)
                        {
                            reachedEnd = true;
                            break;
                        }

                        inputArgBits.Enqueue(input.Dequeue());
                        break;

                    default:
                        inputArgBits.Enqueue(input.Dequeue());
                        break;
                }
            }

            // parse
            ILexToken[] tokenised = this.ParseBitQueue(inputArgBits, impliedBraces: false, trim: true).ToArray();
            return new LexTokenInputArg(tokenised);
        }
    }
}
