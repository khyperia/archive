using System.Collections.Generic;
using ProgComC.Parser;
using ProgComC.TypeIdentifier;

namespace ProgComC.Expressions.Operation
{
    internal class CastOperation : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly ITypeIdentifier _type;
        private readonly IValue _value;

        public CastOperation(CharStream.Mark mark, ITypeIdentifier type, IValue value)
        {
            _mark = mark;
            _type = type;
            _value = value;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents { get { return new[] { _value }; } }

        public IValue Value
        {
            get { return _value; }
        }

        public ITypeIdentifier Type
        {
            get { return _type; }
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return null;
        }

        public string Source()
        {
            return string.Format("({0}){1}", _type, _value.Source());
        }
    }
}