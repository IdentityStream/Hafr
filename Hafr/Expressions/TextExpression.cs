namespace Hafr.Expressions
{
    public class TextExpression : Expression
    {
        public TextExpression(string value)
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
