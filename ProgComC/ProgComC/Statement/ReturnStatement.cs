using System;
using System.Collections.Generic;
using ProgComC.Expressions;
using ProgComC.Expressions.Operation;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    class ReturnStatement : ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _value;

        public ReturnStatement(CharStream.Mark mark, IValue value)
        {
            _mark = mark;
            _value = value;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static ReturnStatement Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf(';'))
            {
                stream.TakeWhitespace();
                return new ReturnStatement(marked, null);
            }
            var value = Operators.Parse(stream);
            if (value == null || stream.TakeIf(';') == false)
                stream.Error("Expected semicolon", false, ";", "}");
            stream.TakeWhitespace();
            return new ReturnStatement(marked, value);
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[] { _value }; }
        }

        public IValue Value
        {
            get { return _value; }
        }

        public string Source()
        {
            return _value == null
                       ? string.Format("return;{0}", Environment.NewLine)
                       : string.Format("return {1};{0}", Environment.NewLine, _value.Source());
        }

        public void Accept<T>(ILineVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public bool Returns()
        {
            return true;
        }
    }
}