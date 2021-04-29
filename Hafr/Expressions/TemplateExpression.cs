using System.Linq;
using Hafr.Evaluation;
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

        public string Evaluate<T>(T model)
        {
            return Evaluator.Evaluate(this, model);
        }

        public override string ToString()
        {
            return string.Concat(Parts.Select(x => x.ToString()));
        }
    }
}
