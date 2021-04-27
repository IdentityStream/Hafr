using Superpower.Model;

namespace Hafr.Expressions
{
    public abstract class Expression
    {
        public static Expression Constant(object constant)
        {
            return new ConstantExpression(constant);
        }

        public static Expression FunctionCall(Position position, string name, Expression[] arguments)
        {
            return new FunctionCallExpression(position, name, arguments);
        }

        public static Expression Property(Position position, string name)
        {
            return new PropertyExpression(position, name);
        }

        public static Expression Template(Expression[] parts)
        {
            return new TemplateExpression(parts);
        }

        public static Expression Text(string value)
        {
            return new TextExpression(value);
        }

        public static Expression Pipe(Position position, Expression left, Expression right)
        {
            return new PipeExpression(position, left, right);
        }

        public abstract override string ToString();
    }
}
