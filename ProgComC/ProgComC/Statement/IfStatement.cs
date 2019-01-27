using System;
using System.Collections.Generic;
using ProgComC.Expressions;
using ProgComC.Expressions.Operation;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    class IfStatement : ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _value;
        private readonly ILine _ifTrue;
        private readonly ILine _ifFalse;

        private IfStatement(CharStream.Mark mark, IValue value, ILine ifTrue, ILine ifFalse)
        {
            _mark = mark;
            _value = value;
            _ifTrue = ifTrue;
            _ifFalse = ifFalse;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static IfStatement Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf('(') == false)
                stream.Error("Expected opening parentheses", false, ";", "}", "{");
            stream.TakeWhitespace();
            var value = Operators.Parse(stream);
            if (value == null || stream.TakeIf(')') == false)
                stream.Error("Expected closing parentheses", false, ";", "}", "{");
            stream.TakeWhitespace();
            var ifTrue = Line.Parse(stream);
            if (ifTrue == null)
                stream.Error("Expected statement or statement body", false, null);
            var ifFalse = (ILine)null;
            if (stream.TakeIf("else"))
            {
                stream.TakeWhitespace();
                ifFalse = Line.Parse(stream);
                if (ifFalse == null)
                    stream.Error("Expected statement or statement body", false, null);
            }
            return new IfStatement(marked, value, ifTrue, ifFalse);
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return _ifFalse == null ? new IAstNode[] { _value, _ifTrue } : new IAstNode[] { _value, _ifTrue, _ifFalse }; }
        }

        public IValue Value
        {
            get { return _value; }
        }

        public ILine IfTrue
        {
            get { return _ifTrue; }
        }

        public ILine IfFalse
        {
            get { return _ifFalse; }
        }

        public string Source()
        {
            return _ifFalse == null
                       ? string.Format("if ({1}){0}{{{0}{2}{0}}}{0}", Environment.NewLine, _value, _ifTrue)
                       : string.Format("if ({1}){0}{{{0}{2}{0}}}{0}else{0}{{{0}{3}{0}}}{0}", Environment.NewLine, _value, _ifTrue, _ifFalse);
        }

        public void Accept<T>(ILineVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public bool Returns()
        {
            return _ifFalse != null && _ifTrue.Returns() && _ifFalse.Returns();
        }
    }
}