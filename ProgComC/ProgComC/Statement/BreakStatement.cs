using System;
using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    internal class BreakStatement : ILine
    {
        private readonly CharStream.Mark _mark;

        public BreakStatement(CharStream.Mark mark)
        {
            _mark = mark;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[0]; } }
        public string Source()
        {
            return string.Format("break;{0}", Environment.NewLine);
        }

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