using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    internal class NegationOperation : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _value;

        public NegationOperation(CharStream.Mark mark, IValue value)
        {
            _mark = mark;
            _value = value;
        }

        public IEnumerable<IAstNode> Contents { get { return new[] {_value}; } }
        public CharStream.Mark Mark { get { return _mark; } }

        public IValue Value
        {
            get { return _value; }
        }

        public string Source()
        {
            return "(-" + _value.Source() + ")";
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            var constant = _value.ConstantFold();
            if (constant is int)
                return -(int) constant;
            return null;
        }
    }
}