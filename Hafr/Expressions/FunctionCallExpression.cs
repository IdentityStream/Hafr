using Superpower.Model;

namespace Hafr.Expressions
{
    public class FunctionCallExpression : Expression
    {
        public FunctionCallExpression(Position position, string name, Expression[] arguments) : base(position)
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }

        public Expression[] Arguments { get; }

        public FunctionCallExpression PipeArgument(Expression argument)
        {
            return new(Position, Name, Arguments.Prepend(argument).ToArray());
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
