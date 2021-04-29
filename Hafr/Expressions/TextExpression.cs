using Superpower.Model;

namespace Hafr.Expressions
{
    public class TextExpression : Expression
    {
        public TextExpression(Position position, string value) : base(position)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }
}
