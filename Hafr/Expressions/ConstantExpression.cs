using Superpower.Model;

namespace Hafr.Expressions
{
    public class ConstantExpression : Expression
    {
        public ConstantExpression(object? value) : base(Position.Empty)
        {
            Value = value;
        }

        public object? Value { get; }

        public override string ToString()
        {
            return Value?.ToString() ?? "<null>";
        }
    }
}
