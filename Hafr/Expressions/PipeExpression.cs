using Superpower.Model;

namespace Hafr.Expressions
{
    public class PipeExpression : Expression
    {
        public PipeExpression(Position position, Expression left, Expression right) : base(position)
        {
            Left = left;
            Right = right;
        }

        public Expression Left { get; }

        public Expression Right { get; }

        public override string ToString()
        {
            return $"{Left} | {Right}";
        }
    }
}
