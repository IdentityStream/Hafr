namespace Hafr.Expressions
{
    public class ConstantExpression : Expression
    {
        public ConstantExpression(object value)
        {
            Value = value;
        }

        public object Value { get; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
