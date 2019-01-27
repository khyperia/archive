using System.Collections.Generic;
using ProgComC.Parser;
using ProgComC.Statement;

namespace ProgComC.Expressions.Operation
{
    class PreDecOperation : IValue, ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _variable;

        public PreDecOperation(CharStream.Mark mark, IValue variable)
        {
            _mark = mark;
            _variable = variable;
        }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[0]; } }
        public CharStream.Mark Mark { get { return _mark; } }

        public IValue Variable
        {
            get { return _variable; }
        }

        public string Source()
        {
            return "--" + _variable.Source();
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return null;
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