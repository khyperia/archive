using System.Collections.Generic;
using ProgComC.Parser;
using ProgComC.Statement;

namespace ProgComC.Expressions.Operation
{
    internal class DereferenceOperation : IValue, IAssignable
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _value;

        public DereferenceOperation(CharStream.Mark mark, IValue value)
        {
            _mark = mark;
            _value = value;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents { get { return new[] { _value }; } }

        public IValue Value
        {
            get { return _value; }
        }

        public string Source()
        {
            return string.Format("(*{0})", _value.Source());
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