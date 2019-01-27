using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ProgComC.Parser;

namespace ProgComC.Expressions.BasicValue
{
    internal class IntegerLiteral : IValue
    {
        private readonly CharStream.Mark _mark;
        private readonly int _value;

        public IntegerLiteral(CharStream.Mark mark, int value)
        {
            _mark = mark;
            _value = value;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static IValue Parse(CharStream stream)
        {
            if (char.IsNumber(stream.Peek()) == false && stream.Peek() != '+' && stream.Peek() != '-')
                return null;
            var marked = stream.MarkPosition();
            var builder = new StringBuilder();
            var sign = stream.TakeIf('+', '-');
            if (sign != default(char))
            {
                stream.TakeWhitespace();
                builder.Append(sign);
            }
            if (stream.TakeIf("0x") || stream.TakeIf("0X"))
            {
                while (true)
                {
                    var c = char.ToLower(stream.Peek());
                    switch (c)
                    {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case 'a':
                        case 'b':
                        case 'c':
                        case 'd':
                        case 'e':
                        case 'f':
                            builder.Append(stream.Take());
                            break;
                        default:
                            stream.TakeWhitespace();
                            return new IntegerLiteral(marked, Convert.ToInt32(builder.ToString(), 16));
                    }
                }
            }
            while (char.IsNumber(stream.Peek()))
                builder.Append(stream.Take());
            stream.TakeWhitespace();
            if (stream.TakeIf('.'))
            {
                stream.TakeWhitespace();
                builder.Append('.');
                while (char.IsNumber(stream.Peek()))
                    builder.Append(stream.Take());
                stream.TakeWhitespace();
                if (stream.TakeIf('e', 'E') != default(char))
                {
                    stream.TakeWhitespace();
                    var exponentSign = stream.TakeIf('+', '-');
                    if (exponentSign != default(char))
                    {
                        stream.TakeWhitespace();
                        builder.Append(exponentSign);
                    }
                    while (char.IsNumber(stream.Peek()))
                        builder.Append(stream.Take());
                    stream.TakeWhitespace();
                }
                if (stream.TakeIf('f', 'F') != default(char))
                    stream.TakeWhitespace();
                return new FloatLiteral(marked, float.Parse(builder.ToString()));
            }
            if (stream.TakeIf('f', 'F') != default(char))
            {
                stream.TakeWhitespace();
                return new FloatLiteral(marked, float.Parse(builder.ToString()));
            }
            return new IntegerLiteral(marked, int.Parse(builder.ToString()));
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return new IAstNode[0]; }
        }

        public int Value
        {
            get { return _value; }
        }

        public string Source()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return _value >= 0 && _value < short.MaxValue ? _value : (object)null;
        }
    }
}
