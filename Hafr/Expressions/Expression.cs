using Superpower.Model;

namespace Hafr.Expressions
{
    public abstract class Expression
    {
        protected Expression(Position position)
        {
            Position = position;
        }

        public Position Position { get; }

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

        public static Expression MultiTemplate(TemplateExpression[] parts)
        {
            return new MultiTemplateExpression(parts);
        }

        public static TemplateExpression Template(Expression[] parts)
        {
            return new TemplateExpression(parts);
        }

        public static Expression Text(Position position, string value)
        {
            return new TextExpression(position, value);
        }

        public static Expression Pipe(Position position, Expression left, Expression right)
        {
            return new PipeExpression(position, left, right);
        }

        public abstract override string ToString();
    }
}
