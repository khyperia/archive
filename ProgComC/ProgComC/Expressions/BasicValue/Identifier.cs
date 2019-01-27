using System.Collections.Generic;
using System.Text;
using ProgComC.Parser;
using ProgComC.Statement;

namespace ProgComC.Expressions.BasicValue
{
    internal class Identifier : IValue, IAssignable
    {
        private readonly CharStream.Mark _mark;
        private readonly string _name;

        public Identifier(CharStream.Mark mark, string name)
        {
            _mark = mark;
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }

        public static Identifier Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (char.IsLetter(stream.Peek()) == false && stream.Peek() != '_')
                return null;
            var builder = new StringBuilder(char.ToString(stream.Take()));
            while (char.IsLetterOrDigit(stream.Peek()) || stream.Peek() == '_')
                builder.Append(stream.Take());
            stream.TakeWhitespace();
            return new Identifier(marked, builder.ToString());
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[0]; }
        }

        public CharStream.Mark Mark
        {
            get { return _mark; }
        }

        public string Source()
        {
            return _name;
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