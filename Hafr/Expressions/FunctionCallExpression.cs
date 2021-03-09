using System.Linq;

namespace Hafr.Expressions
{
    public class FunctionCallExpression : Expression
    {
        public FunctionCallExpression(string name, Expression[] arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }

        public Expression[] Arguments { get; }

        public override string ToString()
        {
            return $"{Name}({string.Join(", ", Arguments.Select(x => x.ToString()))})";
        }
    }
}
