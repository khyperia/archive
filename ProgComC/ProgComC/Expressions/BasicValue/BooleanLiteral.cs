using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Expressions.BasicValue
{
    internal class BooleanLiteral : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly bool _value;

        private BooleanLiteral(CharStream.Mark mark, bool value)
        {
            _mark = mark;
            _value = value;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static BooleanLiteral Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            var keyword = stream.TakeIf("true", "false");
            if (keyword == null)
                return null;
            stream.TakeWhitespace();
            return new BooleanLiteral(marked, keyword == "true");
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[0]; }
        }

        public bool Value
        {
            get { return _value; }
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return _value ? 1 : 0;
        }

        public string Source()
        {
            return _value ? "true" : "false";
        }
    }
}