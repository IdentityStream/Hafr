using Superpower.Model;

namespace Hafr.Expressions
{
    public class TemplateExpression : Expression
    {
        public TemplateExpression(Expression[] parts) : base(Position.Zero)
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
