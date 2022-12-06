using Superpower.Model;

namespace Hafr.Evaluation
{
    public class TemplateEvaluationException : Exception
    {
        public TemplateEvaluationException(string message, Position position) : base(message)
        {
            Position = position;
        }

        public Position Position { get; }
    }
}
