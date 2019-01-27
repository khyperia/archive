using System;
using System.Collections.Generic;
using ProgComC.Expressions;
using ProgComC.Expressions.BasicValue;
using ProgComC.Expressions.Operation;
using ProgComC.Parser;
using ProgComC.TypeIdentifier;

namespace ProgComC.Statement
{
    class VariableDeclarationStatement : ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly ITypeIdentifier _type;
        private readonly Identifier _variable;
        private readonly IValue _value;

        private VariableDeclarationStatement(CharStream.Mark mark, ITypeIdentifier type, Identifier variable, IValue value)
        {
            _mark = mark;
            _type = type;
            _variable = variable;
            _value = value;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public ITypeIdentifier Type
        {
            get { return _type; }
        }

        public Identifier Variable
        {
            get { return _variable; }
        }

        public IValue Value
        {
            get { return _value; }
        }

        public static VariableDeclarationStatement Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            var type = TypeIdentifierParser.Parse(stream);
            if (type == null)
                return null;
            var variable = type.VariableName ?? Identifier.Parse(stream);
            if (variable == null)
            { stream.ResetPosition(marked); return null; }
            if (stream.TakeIf(';'))
            {
                stream.TakeWhitespace();
                return new VariableDeclarationStatement(marked, type, variable, null);
            }
            if (stream.TakeIf('['))
            {
                stream.TakeWhitespace();
                var arraySizeValue = Operators.Parse(stream);
                var arraySizeObj = arraySizeValue == null ? null : arraySizeValue.ConstantFold();
                int arraySize;
                if (arraySizeObj is int)
                    arraySize = (int) arraySizeObj;
                else
                {
                    arraySize = 0;
                    stream.Error("Array size must be constant", false, null);
                }
                if (stream.TakeIf(']') == false)
                { stream.ResetPosition(marked); return null; }
                stream.TakeWhitespace();
                return new VariableDeclarationStatement(marked, new ArrayTypeIdentifier(type, arraySize), variable, null);
            }
            if (stream.TakeIf('=') == false)
            { stream.ResetPosition(marked); return null; }
            stream.TakeWhitespace();
            var value = Operators.Parse(stream);
            if (value == null || stream.TakeIf(';') == false)
            { stream.ResetPosition(marked); return null; }
            stream.TakeWhitespace();
            return new VariableDeclarationStatement(marked, type, variable, value);
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return Value == null ? new IAstNode[] { _variable } : new IAstNode[] { _variable, Value }; }
        }

        public string Source()
        {
            return _value == null
                       ? _type is FunctionPointerType
                             ? string.Format("{0};{1}", _type, Environment.NewLine)
                             : string.Format("{0} {1};{2}", _type, _variable.Source(), Environment.NewLine)
                       : string.Format("{0} {1} = {2};{3}", _type, _variable.Source(), _value.Source(), Environment.NewLine);
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