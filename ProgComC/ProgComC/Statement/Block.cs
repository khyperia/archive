using System.Collections.Generic;
using System.Linq;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    class Block : ILine
    {
        private readonly CharStream.Mark _mark;
        private readonly ILine[] _lines;

        private Block(CharStream.Mark mark, ILine[] lines)
        {
            _mark = mark;
            _lines = lines;
        }

        public CharStream.Mark Mark { get { return _mark; } }

        public static Block Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            var list = new List<ILine>();
            while (true)
            {
                var line = Line.Parse(stream);
                if (line == null)
                    return new Block(marked, list.ToArray());
                list.Add(line);
            }
        }

        public void Accept<T>(ILineVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public bool Returns()
        {
            return _lines.Any(l => l.Returns());
        }

        public IEnumerable<IAstNode> Contents
        {
            get { return _lines; }
        }

        public ILine[] Lines
        {
            get { return _lines; }
        }

        public string Source()
        {
            return string.Concat(_lines.Select(l => l.Source()).ToArray());
        }
    }
}