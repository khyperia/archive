using System.Collections.Generic;
using ProgComC.Parser;
using ProgComC.Statement;

namespace ProgComC.Expressions.Operation
{
    class AssignmentOperation : IValue, ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _target;
        private readonly IValue _value;

        public AssignmentOperation(CharStream.Mark mark, IValue target, IValue value)
        {
            _mark = mark;
            _target = target;
            _value = value;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[] { _value, _target }; }
        }

        public IValue Target
        {
            get { return _target; }
        }

        public IValue Value
        {
            get { return _value; }
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return _value == null ? null : _value.ConstantFold();
        }

        public void Accept<T>(ILineVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public bool Returns()
        {
            return false;
        }

        public string Source()
        {
            return string.Format("{0} = {1}", _target.Source(), _value.Source());
        }
    }
}