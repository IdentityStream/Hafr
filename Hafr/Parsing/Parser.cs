using System.Diagnostics.CodeAnalysis;
using Hafr.Expressions;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Hafr.Parsing
{
    public static class Parser
    {
        private static readonly TokenListParser<TemplateToken, Expression> Number =
            Token.EqualTo(TemplateToken.Number)
                .Apply(Numerics.IntegerInt32)
                .Select(x => Expression.Constant(x));

        private static readonly TokenListParser<TemplateToken, Expression> String =
            Token.EqualTo(TemplateToken.String)
                .Apply(QuotedString.SqlStyle)
                .Select(Expression.Constant);

        private static readonly TokenListParser<TemplateToken, Expression> Property =
            Token.EqualTo(TemplateToken.Identifier)
                .Select(x => Expression.Property(x.Position, x.ToStringValue()));

        private static readonly TokenListParser<TemplateToken, Expression> Argument =
            Number.Or(String).Or(Parse.Ref(() => FunctionCall!)).Try().Or(Property);

        private static readonly TokenListParser<TemplateToken, Expression> FunctionCall =
            from identifier in Token.EqualTo(TemplateToken.Identifier)
            from arguments in Argument.ManyDelimitedBy(Token.EqualTo(TemplateToken.Comma))
                    .Between(Token.EqualTo(TemplateToken.OpenParen), Token.EqualTo(TemplateToken.CloseParen))
            select Expression.FunctionCall(identifier.Position, identifier.ToStringValue(), arguments);

        private static readonly TokenListParser<TemplateToken, Expression> PipedFunctionChain =
            Parse.Chain(Token.EqualTo(TemplateToken.Pipe), Argument,
                (pipe, left, right) => Expression.Pipe(pipe.Position, left, right));

        private static readonly TokenListParser<TemplateToken, Expression> Hole =
            PipedFunctionChain.Between(Token.EqualTo(TemplateToken.OpenCurly), Token.EqualTo(TemplateToken.CloseCurly));

        private static readonly TokenListParser<TemplateToken, Expression> Text =
            Token.EqualTo(TemplateToken.Text).Select(x => Expression.Text(x.Position, x.ToStringValue()));

        private static readonly TokenListParser<TemplateToken, TemplateExpression> Template =
            Hole.Or(Text).AtLeastOnce().Select(Expression.Template);

        private static readonly TokenListParser<TemplateToken, Expression> MultiTemplate =
            Template.AtLeastOnceDelimitedBy(Token.EqualTo(TemplateToken.LineBreak))
                .Select(Expression.MultiTemplate).AtEnd();

        public static bool TryParse(string input, [NotNullWhen(true)] out MultiTemplateExpression? expression, [NotNullWhen(false)] out string? error, out Position errorPosition)
        {
            var tokens = Tokenizer.Instance.TryTokenize(input);
            if (!tokens.HasValue)
            {
                expression = null;
                error = tokens.ToString();
                errorPosition = tokens.ErrorPosition;
                return false;
            }

            var result = MultiTemplate(tokens.Value);
            if (!result.HasValue)
            {
                expression = null;
                error = result.ToString();
                errorPosition = result.ErrorPosition;
                return false;
            }

            expression = (MultiTemplateExpression)result.Value;
            error = null;
            errorPosition = Position.Empty;
            return true;
        }
    }
}
