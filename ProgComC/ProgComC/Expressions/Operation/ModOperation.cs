using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    internal class ModOperation : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _left;
        private readonly IValue _right;

        public ModOperation(CharStream.Mark mark, IValue left, IValue right)
        {
            _mark = mark;
            _left = left;
            _right = right;
        }

        public IEnumerable<IAstNode> Contents { get { return new[] { _left, _right }; } }
        public CharStream.Mark Mark { get { return _mark; } }

        public string Source()
        {
            return string.Format("({0} % {1})", _left.Source(), _right.Source());
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public IValue Left
        {
            get { return _left; }
        }

        public IValue Right
        {
            get { return _right; }
        }

        public object ConstantFold()
        {
            var left = _left.ConstantFold();
            var right = _right.ConstantFold();
            if (left is int && right is int)
                return (int)left % (int)right;
            return null;
        }
    }
}