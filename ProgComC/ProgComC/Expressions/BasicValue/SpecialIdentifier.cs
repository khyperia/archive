using System.Collections.Generic;
using System.Text;
using ProgComC.Parser;
using ProgComC.Statement;

namespace ProgComC.Expressions.BasicValue
{
    internal class SpecialIdentifier : IValue, IAssignable
    {
        private readonly CharStream.Mark _mark;
        private readonly string _name;

        private SpecialIdentifier(CharStream.Mark mark, string name)
        {
            _mark = mark;
            _name = name;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static SpecialIdentifier Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf('$') == false)
                return null;
            stream.TakeWhitespace();
            var builder = new StringBuilder();
            while (char.IsLetterOrDigit(stream.Peek()) || stream.Peek() == '_')
                builder.Append(stream.Take());
            if (builder.Length == 0)
                stream.Error("Expected identifier", true, " ", "\n", "\r");
            stream.TakeWhitespace();
            return new SpecialIdentifier(marked, builder.ToString());
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[0]; }
        }

        public string Source()
        {
            return string.Format("${0}", _name);
        }

        public string Name
        {
            get { return _name; }
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return null;
        }

        public void Accept<T1, T2>(IAssignableVisitor<T1, T2> visitor, T1 assignedValue, T2 data)
        {
            visitor.Visit(this, assignedValue, data);
        }
    }
}