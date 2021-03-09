namespace Hafr.Expressions
{
    public class PropertyExpression : Expression
    {
        public PropertyExpression(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return $"get({Name})";
        }
    }
}
