using System.Collections.Generic;
using System.Linq;

namespace ProgComC.Parser
{
    class CharStream
    {
        private readonly List<CompilerError> _errors;
        private readonly string _filename;
        private readonly string _source;
        private int _index;

        public CharStream(string filename, string s)
        {
            _errors = new List<CompilerError>();
            _filename = filename;
            _source = s;
        }

        public string Filename
        {
            get { return _filename; }
        }

        public List<CompilerError> Errors
        {
            get { return _errors; }
        }

        public bool HasRemaining()
        {
            return _index < _source.Length;
        }

        public char Peek()
        {
            return HasRemaining() ? _source[_index] : '\0';
        }

        public char Take()
        {
            if (HasRemaining() == false)
                return '\0';
            var c = _source[_index];
            _index++;
            return c;
        }

        public bool TakeIf(char x)
        {
            if (HasRemaining() == false || _source[_index] != x)
                return false;
            _index++;
            return true;
        }

        public char TakeIf(params char[] x)
        {
            return x.FirstOrDefault(TakeIf);
        }

        public bool TakeIf(string x)
        {
            if (_index + x.Length >= _source.Length || _source.Substring(_index, x.Length) != x)
                return false;
            _index += x.Length;
            return true;
        }

        public string TakeIf(params string[] x)
        {
            return x.FirstOrDefault(TakeIf);
        }

        public void TakeWhitespace()
        {
            while (_index < _source.Length && char.IsWhiteSpace(_source, _index))
                _index++;
        }

        public string Error(string description, bool takeSkipUntil, params string[] skipUntil)
        {
            var begin = MarkPosition();
            var skippedUntil = (string)null;
            if (skipUntil != null)
            {
                while (HasRemaining() && (skippedUntil = TakeIf(skipUntil)) == null)
                    _index++;
                if (takeSkipUntil == false && skippedUntil != null)
                    _index -= skippedUntil.Length;
                TakeWhitespace();
            }
            var error = new CompilerError(begin, description);
            Errors.Add(error);
            return skippedUntil;
        }

        public Mark MarkPosition()
        {
            return new Mark(_filename, _source, _index);
        }

        public void ResetPosition(Mark marked)
        {
            _index = marked.Position;
        }

        public struct Mark
        {
            private readonly string _file;
            private readonly string _sourcecode;
            private readonly int _position;

            public Mark(string file, string sourcecode, int position)
            {
                _file = file;
                _sourcecode = sourcecode;
                _position = position;
            }

            public string File { get { return _file; } }
            public int Position { get { return _position; } }
            public int Column { get { return _position - _sourcecode.LastIndexOf('\n', _position) - 1; } }
            public int Line { get { return _sourcecode.Take(_position).Count(c => c == '\n') + 1; } }
        }
    }

    struct CompilerError
    {
        private readonly int _line;
        private readonly int _column;
        private readonly string _file;
        private readonly string _description;

        public CompilerError(CharStream.Mark position, string description)
        {
            _description = description;
            _file = position.File;
            _column = position.Column;
            _line = position.Line;
        }

        public int Line
        {
            get { return _line; }
        }

        public int Column
        {
            get { return _column; }
        }

        public string File
        {
            get { return _file; }
        }

        public string Description
        {
            get { return _description; }
        }
    }
}
