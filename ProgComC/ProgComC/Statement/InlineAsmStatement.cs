using System;
using System.Collections.Generic;
using System.Text;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    internal class InlineAsmStatement : ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly string _asm;

        private InlineAsmStatement(CharStream.Mark mark, string source)
        {
            _mark = mark;
            _asm = source;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static InlineAsmStatement Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf('{') == false)
                if (stream.Error("Expected opening brace", true, "{", "}") == "}")
                    return null;
            var builder = new StringBuilder();
            while (true)
            {
                var c = stream.Take();
                switch (c)
                {
                    case '}':
                        stream.TakeWhitespace();
                        return new InlineAsmStatement(marked, builder.ToString());
                    case '\\':
                        builder.Append(stream.Peek() == '}' ? stream.Take() : '\\');
                        break;
                    case default(char):
                        stream.Error("Unexpected end of file", false, null);
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }
        }

        public IEnumerable<IAstNode> Contents { get { return new IAstNode[0]; } }

        public string Asm
        {
            get { return _asm; }
        }

        public string Source()
        {
            return "asm {" + _asm + "}" + Environment.NewLine;
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