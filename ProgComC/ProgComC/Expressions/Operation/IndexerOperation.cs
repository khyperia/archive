using System.Collections.Generic;
using ProgComC.Parser;
using ProgComC.Statement;

namespace ProgComC.Expressions.Operation
{
    internal class IndexerOperation : IValue, IAssignable
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _array;
        private readonly IValue _index;

        public IndexerOperation(CharStream.Mark mark, IValue array, IValue index)
        {
            _mark = mark;
            _array = array;
            _index = index;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents { get { return new[] { _array, _index }; } }

        public IValue Array
        {
            get { return _array; }
        }

        public IValue Index
        {
            get { return _index; }
        }

        public string Source()
        {
            return string.Format("{0}[{1}]", _array.Source(), _index.Source());
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
