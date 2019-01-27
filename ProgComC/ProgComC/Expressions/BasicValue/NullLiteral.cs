using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Expressions.BasicValue
{
    internal class NullLiteral : IValue
    {
        private readonly CharStream.Mark _mark;

        private NullLiteral(CharStream.Mark mark)
        {
            _mark = mark;
        }

        public static NullLiteral Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf("null") == false)
                return null;
            stream.TakeWhitespace();
            return new NullLiteral(marked);
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[0]; } }
        public string Source()
        {
            return "null";
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return null;
        }
    }
}