using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Hafr.Parsing
{
    public sealed class Tokenizer : Tokenizer<TemplateToken>
    {
        public static readonly Tokenizer Instance = new();

        private Tokenizer()
        {
        }

        protected override IEnumerable<Result<TemplateToken>> Tokenize(TextSpan span, TokenizationState<TemplateToken> state)
        {
            var next = SkipWhiteSpace(span);

            var inHole = false;

            while (next.HasValue)
            {
                switch (next.Value)
                {
                    case '{' when !inHole:
                        yield return SimpleToken(ref next, TemplateToken.OpenCurly, skipWhiteSpace: true);
                        inHole = true;
                        continue;
                    case '}' when inHole:
                        yield return SimpleToken(ref next, TemplateToken.CloseCurly, skipWhiteSpace: false);
                        inHole = false;
                        continue;
                    default:
                        yield return Tokenize(ref next, inHole);
                        break;
                }
            }
        }

        private static Result<TemplateToken> Tokenize(ref Result<char> next, bool inHole)
        {
            if (inHole)
            {
                return TokenizeHole(ref next);
            }

            if (next.Value is '\r' or '\n')
            {
                return Parse(ref next, TemplateToken.LineBreak, Character.WhiteSpace);
            }

            return ConsumeUtil(ref next, TemplateToken.Text, '{');

        }

        private static Result<TemplateToken> TokenizeHole(ref Result<char> next)
        {
            if (next.Value == '(')
            {
                return SimpleToken(ref next, TemplateToken.OpenParen, skipWhiteSpace: true);
            }

            if (next.Value == ')')
            {
                return SimpleToken(ref next, TemplateToken.CloseParen, skipWhiteSpace: true);
            }

            if (next.Value == ',')
            {
                return SimpleToken(ref next, TemplateToken.Comma, skipWhiteSpace: true);
            }

            if (next.Value == '|')
            {
                return SimpleToken(ref next, TemplateToken.Pipe, skipWhiteSpace: true);
            }

            if (next.Value == '\'')
            {
                return Parse(ref next, TemplateToken.String, QuotedString.SqlStyle);
            }

            if (char.IsDigit(next.Value))
            {
                return Parse(ref next, TemplateToken.Number, Numerics.IntegerInt32);
            }

            return Parse(ref next, TemplateToken.Identifier, Identifier.CStyle);
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

            while (next.HasValue && next.Value != end && next.Value != '\r' && next.Value != '\n')
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
    }
}
