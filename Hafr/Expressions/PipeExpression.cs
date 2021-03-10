using Superpower.Model;

namespace Hafr.Expressions
{
    public class PipeExpression : Expression
    {
        public PipeExpression(Position position, Expression left, Expression right)
        {
            Position = position;
            Left = left;
            Right = right;
        }

        public Position Position { get; }

        public Expression Left { get; }

        public Expression Right { get; }

        public override string ToString()
        {
            return $"{Left} | {Right}";
        }
    }
}
