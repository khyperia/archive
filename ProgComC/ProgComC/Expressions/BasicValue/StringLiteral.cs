using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProgComC.Parser;

namespace ProgComC.Expressions.BasicValue
{
    class StringLiteral : IValue
    {
        private static readonly Dictionary<char, char> Escapes = new Dictionary<char, char>
                                                            {
                                                                {'"', '"'},
                                                                {'\'', '\''},
                                                                {'a', '\a'},
                                                                {'b', '\b'},
                                                                {'f', '\f'},
                                                                {'n', '\n'},
                                                                {'r', '\r'},
                                                                {'t', '\t'},
                                                                {'v', '\v'},
                                                                {'\\', '\\'}
                                                            };

        private readonly CharStream.Mark _mark;
        private readonly string _value;

        private StringLiteral(CharStream.Mark mark, string value)
        {
            _mark = mark;
            _value = value;
        }

        public static IValue Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            var open = stream.TakeIf('\'', '"');
            if (open == default(char))
                return null;
            var builder = new StringBuilder();
            while (true)
            {
                var c = stream.Take();
                switch (c)
                {
                    case '\\':
                        var escape = stream.Take();
                        if (Escapes.ContainsKey(escape))
                            builder.Append(Escapes[escape]);
                        else
                        {
                            stream.Error("Unknown character escape sequence '\\" + escape + "'", false, null);
                            builder.Append("\\" + escape);
                        }
                        break;
                    case '\'':
                    case '"':
                        if (c == open)
                        {
                            stream.TakeWhitespace();
                            var value = builder.ToString();
                            if (open == '"')
                                return new StringLiteral(marked, value);
                            if (value.Length != 1)
                                stream.Error("Char literal cannot be more/less than one character long", false, null);
                            return new IntegerLiteral(marked, value.Length == 0 ? 0 : value[0]);
                        }
                        builder.Append(c);
                        break;
                    case default(char):
                        stream.Error("Unexpected EOF", false, null);
                        return new StringLiteral(marked, builder.ToString());
                    case '\r':
                    case '\n':
                        stream.Error("Unexpected newline", false, null);
                        stream.TakeWhitespace();
                        return new StringLiteral(marked, builder.ToString());
                    default:
                        builder.Append(c);
                        break;
                }
            }
        }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[0]; } }
        public CharStream.Mark Mark { get { return _mark; } }

        public string Value
        {
            get { return _value; }
        }

        public string Source()
        {
            return string.Format("\"{0}\"", Escapes.Aggregate(_value, (current, escape) => current.Replace(char.ToString(escape.Value), "\\" + escape.Key)));
        }

        public T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data)
        {
            return visitor.Visit(this, data);
        }

        public object ConstantFold()
        {
            return _value;
        }
    }
}
