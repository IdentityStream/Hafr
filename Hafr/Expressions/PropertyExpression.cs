using Superpower.Model;

namespace Hafr.Expressions
{
    public class PropertyExpression : Expression
    {
        public PropertyExpression(Position position, string name) : base(position)
        {
            Name = name;
        }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
