using System.Collections.Generic;
using ProgComC.Expressions.BasicValue;
using ProgComC.Parser;
using ProgComC.Statement;

namespace ProgComC.Expressions.Operation
{
    class DotOperation : IValue, IAssignable
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _obj;
        private readonly Identifier _fieldname;

        public DotOperation(CharStream.Mark mark, IValue obj, Identifier fieldname)
        {
            _mark = mark;
            _obj = obj;
            _fieldname = fieldname;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public IEnumerable<IAstNode> Contents { get { return new[] { _obj, _fieldname }; } }

        public Identifier Fieldname
        {
            get { return _fieldname; }
        }

        public IValue Obj
        {
            get { return _obj; }
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
            return string.Format("{0}.{1}", _obj.Source(), _fieldname.Source());
        }

        public void Accept<T1, T2>(IAssignableVisitor<T1, T2> visitor, T1 assignedValue, T2 data)
        {
            visitor.Visit(this, assignedValue, data);
        }
    }
}