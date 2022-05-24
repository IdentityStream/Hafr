using System.Collections.Generic;
using System.Linq;
using Hafr.Evaluation;
using Superpower.Model;

namespace Hafr.Expressions
{
    public class MultiTemplateExpression : Expression
    {
        public MultiTemplateExpression(TemplateExpression[] parts) : base(Position.Zero)
        {
            Parts = parts;
        }

        public TemplateExpression[] Parts { get; }

        public IEnumerable<string> Evaluate<T>(T model) where T : notnull
        {
            return Evaluator.Evaluate(this, model);
        }

        public override string ToString()
        {
            return string.Concat(Parts.Select(x => x.ToString()));
        }
    }
}
