using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    internal abstract class ComparisionOperation : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _left;
        private readonly IValue _right;

        protected ComparisionOperation(CharStream.Mark mark, IValue left, IValue right)
        {
            _mark = mark;
            _left = left;
            _right = right;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents { get { return new[] { _left, _right }; } }


        public object ConstantFold()
        {
            var left = _left.ConstantFold();
            var right = _right.ConstantFold();
            if (left is int && right is int)
                return ConstantFold((int) left, (int) right) ? 1 : 0;
            return null;
        }

        protected abstract bool ConstantFold(int left, int right);
        public abstract T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data);
        protected abstract string Operator { get; }

        public IValue Left
        {
            get { return _left; }
        }

        public IValue Right
        {
            get { return _right; }
        }


        public string Source()
        {
            return string.Format("({0} {1} {2})", _left.Source(), Operator, _right.Source());
        }
    }
}