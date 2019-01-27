using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.Expressions.Operation
{
    abstract class BinaryOperation : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _left;
        private readonly IValue _right;

        protected BinaryOperation(CharStream.Mark mark, IValue left, IValue right)
        {
            _mark = mark;
            _left = left;
            _right = right;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[] { _left, _right }; }
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            var left = _left.ConstantFold();
            var right = _right.ConstantFold();
            if (left is int == false || right is int == false)
                return null;
            switch (Opcode)
            {
                case "add": return (int)left + (int)right;
                case "sub": return (int)left - (int)right;
                case "mul": return (int)left * (int)right;
                case "div": return (int)left / (int)right;
                case "shl": return (int)left << (int)right;
                case "shr": return (int)left >> (int)right;
                case "and": return (int)left & (int)right;
                case "or": return (int)left | (int)right;
                case "xor": return (int)left ^ (int)right;
                default: return null;
            }
        }

        public abstract string Opcode { get; }
        public abstract string ImmediateOpcode { get; }
        public abstract bool OrderMatters { get; }

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
            string op;
            switch (Opcode)
            {
                case "add": op = "+"; break;
                case "sub": op = "-"; break;
                case "mul": op = "*"; break;
                case "div": op = "/"; break;
                case "shl": op = "<<"; break;
                case "shr": op = ">>"; break;
                case "and": op = "&"; break;
                case "or": op = "|"; break;
                case "xor": op = "^"; break;
                default: op = "?"; break;
            }
            return string.Format("({0} {1} {2})", _left.Source(), op, _right.Source());
        }
    }
}