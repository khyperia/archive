using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    internal class BitwiseInversionOperation : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _value;

        public BitwiseInversionOperation(CharStream.Mark mark, IValue value)
        {
            _mark = mark;
            _value = value;
        }

        public IEnumerable<IAstNode> Contents { get { return new[] {_value}; } }
        public CharStream.Mark Mark { get { return _mark; } }

        public string Source()
        {
            return string.Format("(~{0})", _value);
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public IValue Value
        {
            get { return _value; }
        }

        public object ConstantFold()
        {
            var obj = _value.ConstantFold();
            if (obj is int)
                return ~(int) obj;
            return null;
        }
    }
}