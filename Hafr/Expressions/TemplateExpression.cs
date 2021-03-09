using System.Linq;

namespace Hafr.Expressions
{
    public class TemplateExpression : Expression
    {
        public TemplateExpression(Expression[] parts)
        {
            Parts = parts;
        }

        public Expression[] Parts { get; }

        public override string ToString()
        {
            return string.Concat(Parts.Select(x => x.ToString()));
        }
    }
}
