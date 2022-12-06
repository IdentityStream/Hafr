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

        public IEnumerable<string> EvaluateModel<T>(T model) where T : notnull
        {
            return Evaluator.EvaluateModel(this, model);
        }

        public IEnumerable<string> EvaluateModel(object model)
        {
            return Evaluator.EvaluateModel(this, model);
        }

        public IEnumerable<string> EvaluateModel(object model, Type? modelType)
        {
            return Evaluator.EvaluateModel(this, model, modelType);
        }

        public IEnumerable<string> EvaluateProperties(IDictionary<string, object?> properties)
        {
            return Evaluator.EvaluateProperties(this, properties);
        }

        public override string ToString()
        {
            return string.Concat(Parts.Select(x => x.ToString()));
        }
    }
}
