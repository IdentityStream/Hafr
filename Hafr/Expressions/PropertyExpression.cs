using Superpower.Model;

namespace Hafr.Expressions
{
    public class PropertyExpression : Expression
    {
        public PropertyExpression(Position position, string name)
        {
            Position = position;
            Name = name;
        }

        public Position Position { get; }

        public string Name { get; }

        public override string ToString()
        {
            return $"get({Name})";
        }
    }
}
