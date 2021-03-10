using System.Collections.Generic;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Hafr.Parsing
{
    public class Tokenizer : Tokenizer<TemplateToken>
    {
        public static readonly Tokenizer Instance = new();

        private Tokenizer()
        {
        }

        protected override IEnumerable<Result<TemplateToken>> Tokenize(TextSpan span, TokenizationState<TemplateToken> state)
        {
            var next = SkipWhiteSpace(span);

            while (next.HasValue)
            {
                yield return Tokenize(ref next, state);
            }
        }

        private static Result<TemplateToken> Tokenize(ref Result<char> next, TokenizationState<TemplateToken> state)
        {
            return next.Value switch
            {
                '{' => SimpleToken(ref next, TemplateToken.OpenCurly, skipWhiteSpace: true),
                '}' => SimpleToken(ref next, TemplateToken.CloseCurly, skipWhiteSpace: false),
                '(' => SimpleToken(ref next, TemplateToken.OpenParen, skipWhiteSpace: true),
                ')' => SimpleToken(ref next, TemplateToken.CloseParen, skipWhiteSpace: true),
                ',' => SimpleToken(ref next, TemplateToken.Comma, skipWhiteSpace: true),
                '|' => SimpleToken(ref next, TemplateToken.Pipe, skipWhiteSpace: true),
                  _ => ComplexToken(ref next, state)
            };
        }

        private static Result<TemplateToken> ComplexToken(ref Result<char> next, TokenizationState<TemplateToken> state)
        {
            if (ExpectIdentifier(state.Previous))
            {
                if (char.IsDigit(next.Value))
                {
                    return Parse(ref next, TemplateToken.Number, Numerics.IntegerInt32);
                }

                if (next.Value == '\'')
                {
                    return Parse(ref next, TemplateToken.String, QuotedString.SqlStyle);
                }

                return Parse(ref next, TemplateToken.Identifier, Identifier.CStyle);
            }

            return ConsumeUtil(ref next, TemplateToken.Text, '{');
        }

        private static Result<TemplateToken> Parse<T>(ref Result<char> next, TemplateToken token, TextParser<T> parser)
        {
            var result = parser(next.Location);

            if (!result.HasValue)
            {
                return Result.CastEmpty<T, TemplateToken>(result);
            }

            next = SkipWhiteSpace(result.Remainder);

            return Result.Value(token, result.Location, result.Remainder);
        }

        private static Result<TemplateToken> ConsumeUtil(ref Result<char> next, TemplateToken token, char end)
        {
            var begin = next.Location;

            while (next.HasValue && next.Value != end)
            {
                next = next.Remainder.ConsumeChar();
            }

            return Result.Value(token, begin, next.Location);
        }

        private static Result<TemplateToken> SimpleToken(ref Result<char> next, TemplateToken token, bool skipWhiteSpace)
        {
            var result = Result.Value(token, next.Location, next.Remainder);
            next = skipWhiteSpace ? SkipWhiteSpace(next.Remainder) : next.Remainder.ConsumeChar();
            return result;
        }

        private static bool ExpectIdentifier(Token<TemplateToken>? previous) => previous switch
        {
            { Kind: TemplateToken.OpenCurly } => true,
            { Kind: TemplateToken.OpenParen } => true,
            { Kind: TemplateToken.Comma } => true,
            { Kind: TemplateToken.Pipe } => true,
            _ => false
        };
    }
}
