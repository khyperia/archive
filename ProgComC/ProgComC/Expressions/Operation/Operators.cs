using System;
using System.Collections.Generic;
using ProgComC.Expressions.BasicValue;
using ProgComC.Parser;
using ProgComC.TypeIdentifier;

namespace ProgComC.Expressions.Operation
{
    static class Operators
    {
        public static IValue Parse(CharStream stream)
        {
            var value = BitTwiddling(stream);
            if (value == null)
                return null;
            var marked = stream.MarkPosition();
            while (true)
            {
                if (stream.TakeIf('=') == false)
                    return value;
                stream.TakeWhitespace();
                var nextValue = BitTwiddling(stream);
                if (nextValue == null)
                    stream.Error("Expected value", false, ")", ";", "}");
                value = new AssignmentOperation(marked, value, nextValue);
            }
        }

        private static IValue BitTwiddling(CharStream stream)
        {
            var value = Equality(stream);
            if (value == null)
                return null;
            var marked = stream.MarkPosition();
            while (true)
            {
                var c = stream.TakeIf('&', '|', '^');
                if (c == default(char))
                    return value;
                stream.TakeWhitespace();
                var nextValue = Equality(stream);
                if (nextValue == null)
                    stream.Error("Expected value", false, ")", ";", "}");
                switch (c)
                {
                    case '&':
                        value = new AndOperation(marked, value, nextValue);
                        break;
                    case '|':
                        value = new OrOperation(marked, value, nextValue);
                        break;
                    default:
                        value = new XorOperation(marked, value, nextValue);
                        break;
                }
            }
        }

        private static IValue Equality(CharStream stream)
        {
            var value = Shift(stream);
            if (value == null)
                return null;
            var marked = stream.MarkPosition();
            while (true)
            {
                var c = stream.TakeIf("==", "!=", "<=", ">=", "<", ">");
                if (c == default(string))
                    return value;
                stream.TakeWhitespace();
                var nextValue = Shift(stream);
                if (nextValue == null)
                    stream.Error("Expected value", false, ")", ";", "}");
                switch (c)
                {
                    case "==": value = new EqualityOperation(marked, value, nextValue); break;
                    case "!=": value = new InequalityOperation(marked, value, nextValue); break;
                    case "<=": value = new LessThanOrEqualOperation(marked, value, nextValue); break;
                    case ">=": value = new GreaterThanOrEqualOperation(marked, value, nextValue); break;
                    case "<": value = new LessThanOperation(marked, value, nextValue); break;
                    case ">": value = new GreaterThanOperation(marked, value, nextValue); break;
                }
            }
        }

        private static IValue Shift(CharStream stream)
        {
            var value = AddSub(stream);
            if (value == null)
                return null;
            var marked = stream.MarkPosition();
            while (true)
            {
                var c = stream.TakeIf(">>", "<<");
                if (c == default(string))
                    return value;
                stream.TakeWhitespace();
                var nextValue = AddSub(stream);
                if (nextValue == null)
                    stream.Error("Expected value", false, ")", ";", "}");
                if (c == ">>")
                    value = new ShiftRightOperation(marked, value, nextValue);
                else
                    value = new ShiftLeftOperation(marked, value, nextValue);
            }
        }

        private static IValue AddSub(CharStream stream)
        {
            var value = MulDiv(stream);
            if (value == null)
                return null;
            var marked = stream.MarkPosition();
            while (true)
            {
                var c = stream.TakeIf('+', '-');
                if (c == default(char))
                    return value;
                stream.TakeWhitespace();
                var nextValue = MulDiv(stream);
                if (nextValue == null)
                    stream.Error("Expected value", false, ")", ";", "}");
                if (c == '+')
                    value = new AddOperation(marked, value, nextValue);
                else
                    value = new SubOperation(marked, value, nextValue);
            }
        }

        private static IValue MulDiv(CharStream stream)
        {
            var value = Unary(stream);
            if (value == null)
                return null;
            var marked = stream.MarkPosition();
            while (true)
            {
                var c = stream.TakeIf('*', '/', '%');
                if (c == default(char))
                    return value;
                stream.TakeWhitespace();
                var nextValue = Unary(stream);
                if (nextValue == null)
                    stream.Error("Expected value", false, ")", ";", "}");
                switch (c)
                {
                    case '*':
                        value = new MulOperation(marked, value, nextValue);
                        break;
                    case '/':
                        value = new DivOperation(marked, value, nextValue);
                        break;
                    default:
                        value = new ModOperation(marked, value, nextValue);
                        break;
                }
            }
        }

        private static IValue Unary(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf('('))
            {
                stream.TakeWhitespace();
                var type = TypeIdentifierParser.Parse(stream);
                if (type != null && stream.TakeIf(')'))
                {
                    stream.TakeWhitespace();
                    return new CastOperation(marked, type, Unary(stream));
                }
                stream.ResetPosition(marked);
            }
            if (stream.TakeIf("++"))
            {
                stream.TakeWhitespace();
                return new PreIncOperation(marked, Unary(stream));
            }
            if (stream.TakeIf("--"))
            {
                stream.TakeWhitespace();
                return new PreDecOperation(marked, Unary(stream));
            }
            var unaryOperator = stream.TakeIf('*', '&', '!', '-', '~');
            if (unaryOperator == default(char))
                return Primary(stream, BasicValue.BasicValue.Parse(stream));
            stream.TakeWhitespace();
            var next = Unary(stream);
            switch (unaryOperator)
            {
                case '*':
                    return new DereferenceOperation(marked, next);
                case '&':
                    return new AddressOfOperation(marked, next);
                case '!':
                    return new NotOperation(marked, next);
                case '-':
                    return new NegationOperation(marked, next);
                case '~':
                    return new BitwiseInversionOperation(marked, next);
                default:
                    throw new Exception("Internal exception: Unknown unary operator");
            }
        }

        private static IValue Primary(CharStream stream, IValue workingOn)
        {
            if (workingOn == null)
                return null;
            var marked = stream.MarkPosition();
            if (stream.TakeIf('('))
            {
                stream.TakeWhitespace();
                var parameterList = new List<IValue>();
                while (true)
                {
                    var value = Parse(stream);
                    if (value == null)
                    {
                        if (parameterList.Count != 0)
                            stream.Error("Expected value", false, ")", ";", "}");
                        break;
                    }
                    parameterList.Add(value);
                    if (stream.TakeIf(',') == false)
                        break;
                    stream.TakeWhitespace();
                }
                if (stream.TakeIf(')') == false)
                    stream.Error("Expected closing parentheses", false, ")", ";", "}");
                stream.TakeWhitespace();
                workingOn = new MethodCall(marked, workingOn, parameterList.ToArray());
                return Primary(stream, workingOn);
            }
            if (stream.TakeIf('.'))
            {
                stream.TakeWhitespace();
                var field = Identifier.Parse(stream);
                if (field == null)
                    stream.Error("Expected identifier", false, ".", ")", ";", "}");
                workingOn = new DotOperation(marked, workingOn, field);
                return Primary(stream, workingOn);
            }
            if (stream.TakeIf("->"))
            {
                stream.TakeWhitespace();
                var field = Identifier.Parse(stream);
                if (field == null)
                    stream.Error("Expected identifier", false, ".", ")", ";", "}");
                workingOn = new DotOperation(marked, new DereferenceOperation(marked, workingOn), field);
                return Primary(stream, workingOn);
            }
            if (stream.TakeIf('['))
            {
                stream.TakeWhitespace();
                var value = Parse(stream);
                if (stream.TakeIf(']') == false)
                    stream.Error("Expected closing square brace", false, ")", ";", "}");
                stream.TakeWhitespace();
                var indexer = new IndexerOperation(marked, workingOn, value);
                return Primary(stream, indexer);
            }
            if (stream.TakeIf("++"))
            {
                stream.TakeWhitespace();
                workingOn = new PostIncOperation(marked, workingOn);
                return Primary(stream, workingOn);
            }
            if (stream.TakeIf("--"))
            {
                stream.TakeWhitespace();
                workingOn = new PostDecOperation(marked, workingOn);
                return Primary(stream, workingOn);
            }
            return workingOn;
        }
    }
}
