using System.Linq;
using Hafr.Evaluation;

namespace Hafr.Expressions
{
    public class TemplateExpression : Expression
    {
        public TemplateExpression(Expression[] parts)
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
