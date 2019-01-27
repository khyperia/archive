using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    internal class VoidStatement : ILine
    {
        private readonly CharStream.Mark _mark;

        public VoidStatement(CharStream.Mark mark)
        {
            _mark = mark;
        }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[0]; } }
        public string Source()
        {
            return "";
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public void Accept<T>(ILineVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public bool Returns()
        {
            return false;
        }
    }
}