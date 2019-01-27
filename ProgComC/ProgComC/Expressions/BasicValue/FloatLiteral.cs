using System.Collections.Generic;
using System.Globalization;
using ProgComC.Parser;

namespace ProgComC.Expressions.BasicValue
{
    internal class FloatLiteral : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly float _value;

        public FloatLiteral(CharStream.Mark mark, float value)
        {
            _mark = mark;
            _value = value;
        }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[0]; } }
        public CharStream.Mark Mark { get { return _mark; } }

        public float Value
        {
            get { return _value; }
        }

        public string Source()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return _value;
        }
    }
}