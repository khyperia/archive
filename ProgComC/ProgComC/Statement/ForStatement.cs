using System;
using System.Collections.Generic;
using ProgComC.Expressions;
using ProgComC.Expressions.Operation;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    internal class ForStatement : ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly ILine _init;
        private readonly IValue _condition;
        private readonly IValue _increment;
        private readonly ILine _block;

        private ForStatement(CharStream.Mark mark, ILine init, IValue condition, IValue increment, ILine block)
        {
            _mark = mark;
            _init = init;
            _condition = condition;
            _increment = increment;
            _block = block;
        }

        public static ForStatement Parse(CharStream stream)
        {
            var mark = stream.MarkPosition();
            if (stream.TakeIf('(') == false)
            {
                stream.Error("Expected opening parenthases", false, ";", "}");
                return new ForStatement(mark, null, null, null, null);
            }
            stream.TakeWhitespace();
            var init = Line.Parse(stream);
            var condition = Operators.Parse(stream);
            if (stream.TakeIf(';') == false)
                stream.Error("Expected semicolon", false, null);
            stream.TakeWhitespace();
            var increment = Operators.Parse(stream);
            if (stream.TakeIf(')') == false)
            {
                stream.Error("Expected closing parenthases", false, ";", "}");
                return new ForStatement(mark, init, condition, increment, null);
            }
            stream.TakeWhitespace();
            var block = Line.Parse(stream);
            return new ForStatement(mark, init, condition, increment, block);
        }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[] { Init, Condition, Increment, Block }; } }
        public CharStream.Mark Mark { get { return _mark; } }

        public ILine Init
        {
            get { return _init; }
        }

        public IValue Condition
        {
            get { return _condition; }
        }

        public IValue Increment
        {
            get { return _increment; }
        }

        public ILine Block
        {
            get { return _block; }
        }

        public string Source()
        {
            return string.Format("for ({0}; {1}; {2}) {3}", Init, Condition, Increment, Block);
        }

        public void Accept<T>(ILineVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public bool Returns()
        {
            return Block.Returns();
        }
    }
}