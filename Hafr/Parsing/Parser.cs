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
                .Select(x => Expression.Property(x.ToStringValue()));

        private static readonly TokenListParser<TemplateToken, Expression> Argument =
            Number.Or(String).Or(Parse.Ref(() => FunctionCall)).Try().Or(Property);

        private static readonly TokenListParser<TemplateToken, Expression> FunctionCall =
            from identifier in Token.EqualTo(TemplateToken.Identifier).Select(x => x.ToStringValue())
            from open in Token.EqualTo(TemplateToken.OpenParen)
            from arguments in Argument
                .ManyDelimitedBy(
                    Token.EqualTo(TemplateToken.Comma),
                    end: Token.EqualTo(TemplateToken.CloseParen))
            select Expression.FunctionCall(identifier, arguments);

        private static readonly TokenListParser<TemplateToken, Expression> Hole =
            from open in Token.EqualTo(TemplateToken.OpenCurly)
            from content in Argument
            from close in Token.EqualTo(TemplateToken.CloseCurly)
            select content;

        private static readonly TokenListParser<TemplateToken, Expression> Text =
            Token.EqualTo(TemplateToken.Text)
                .Select(x => Expression.Text(x.ToStringValue()));

        private static readonly TokenListParser<TemplateToken, TemplateExpression> Template =
            Hole.Or(Text).AtLeastOnce()
                .Select(Expression.Template).AtEnd();

        public static bool TryParse(TokenList<TemplateToken> tokens, out TemplateExpression expr, out string error, out Position errorPosition)
        {
            var result = Template(tokens);
            if (!result.HasValue)
            {
                expr = null;
                error = result.ToString();
                errorPosition = result.ErrorPosition;
                return false;
            }

            expr = result.Value;
            error = null;
            errorPosition = Position.Empty;
            return true;
        }
    }
}
