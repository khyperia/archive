using System.Collections.Generic;
using System.Linq;
using ProgComC.Parser;
using ProgComC.Statement;

namespace ProgComC.Expressions.Operation
{
    class MethodCall : IValue, ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _methodName;
        private readonly IValue[] _parameters;

        public MethodCall(CharStream.Mark mark, IValue methodName, IValue[] parameters)
        {
            _mark = mark;
            _methodName = methodName;
            _parameters = parameters;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[] { _methodName }.Concat(_parameters); }
        }

        public IValue[] Parameters
        {
            get { return _parameters; }
        }

        public IValue MethodName
        {
            get { return _methodName; }
        }

        public string Source()
        {
            return string.Format("{0}({1})", _methodName.Source(),
                                 string.Join(", ", _parameters.Select(p => p.Source()).ToArray()));
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return null;
        }

        public void Accept<T>(ILineVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public bool Returns()
        {
            return false;
        }
    }
}