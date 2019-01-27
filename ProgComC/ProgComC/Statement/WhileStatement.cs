using System;
using System.Collections.Generic;
using ProgComC.Expressions;
using ProgComC.Expressions.Operation;
using ProgComC.HighLevelContent;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    class WhileStatement : ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly IValue _value;
        private readonly ILine _contents;
        public string ConditionLabel { get; set; }

        private WhileStatement(CharStream.Mark mark, IValue value, ILine contents)
        {
            _mark = mark;
            _value = value;
            _contents = contents;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static WhileStatement Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf('(') == false)
                stream.Error("Expected opening parentheses", false, ";", "}", "{");
            stream.TakeWhitespace();
            var value = Operators.Parse(stream);
            if (value == null || stream.TakeIf(')') == false)
                stream.Error("Expected closing parentheses", false, ";", "}", "{");
            stream.TakeWhitespace();
            var contents = Line.Parse(stream);
            if (contents == null)
                stream.Error("Expected statement or statement body", false, null);
            return new WhileStatement(marked, value, contents);
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[] { _value, _contents }; }
        }

        public string Source()
        {
            return string.Format("while ({1}){0}{{{0}{2}{0}}}{0}", Environment.NewLine, _value, _contents);
        }

        public IValue Value
        {
            get { return _value; }
        }

        public ILine BodyContents
        {
            get { return _contents; }
        }

        public string EndLabel { get; private set; }

        public string GetEndLabel(CompilerContext context)
        {
            return EndLabel ?? (EndLabel = context.GenerateLabel());
        }

        public void Accept<T>(ILineVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public bool Returns()
        {
            var constantFold = _value.ConstantFold();
            return constantFold is int && (int)constantFold != 0 && _contents.Returns();
        }
    }
}