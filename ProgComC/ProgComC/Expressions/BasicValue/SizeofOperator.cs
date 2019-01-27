using System.Collections.Generic;
using ProgComC.Parser;
using ProgComC.TypeIdentifier;

namespace ProgComC.Expressions.BasicValue
{
    internal class SizeofOperator : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly ITypeIdentifier _type;

        private SizeofOperator(CharStream.Mark mark, ITypeIdentifier type)
        {
            _mark = mark;
            _type = type;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static SizeofOperator Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf("sizeof") == false)
                return null;
            stream.TakeWhitespace();
            if (stream.TakeIf('(') == false)
                if (stream.Error("Expected opening parentheses", true, ")", ";", "}") != "(")
                    return null;
            var type = TypeIdentifierParser.Parse(stream);
            if (type == null)
                stream.Error("Expected type identifier", false, ")", ";", "}");
            if (stream.TakeIf(')') == false)
                stream.Error("Expected closing parentheses", true, ")");
            stream.TakeWhitespace();
            return new SizeofOperator(marked, type);
        }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[0]; } }

        public ITypeIdentifier Type
        {
            get { return _type; }
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return null;
        }

        public string Source()
        {
            return string.Format("sizeof({0})", _type);
        }
    }
}