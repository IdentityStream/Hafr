namespace Hafr.Expressions
{
    public abstract class Expression
    {
        public static Expression Constant(object constant)
        {
            return new ConstantExpression(constant);
        }

        public static Expression FunctionCall(string name, Expression[] arguments)
        {
            return new FunctionCallExpression(name, arguments);
        }

        public static Expression Property(string name)
        {
            return new PropertyExpression(name);
        }

        public static Expression Template(Expression[] parts)
        {
            return new TemplateExpression(parts);
        }

        public abstract override string ToString();
    }
}
