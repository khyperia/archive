using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProgComC.Parser;

namespace ProgComC.HighLevelContent
{
    class CompilerContext
    {
        private readonly Func<string, string> _fileReader;
        private readonly Action<string, string> _fileWriter;
        private readonly List<string> _parsedFiles;
        private readonly List<IHighLevelContent> _highLevelContents;
        private readonly List<CompilerError> _errors;
        private readonly StringBuilder _emitted;
        private int _currentFreeLabel;

        public CompilerContext(Func<string, string> fileReader, Action<string, string> fileWriter)
        {
            _fileReader = fileReader;
            _fileWriter = fileWriter;
            _parsedFiles = new List<string>();
            _highLevelContents = new List<IHighLevelContent>();
            _errors = new List<CompilerError>();
            _emitted = new StringBuilder();
            _currentFreeLabel = 1;
        }

        public List<string> ParsedFiles
        {
            get { return _parsedFiles; }
        }

        public IEnumerable<IHighLevelContent> AccessableHighLevelContents(string fromFilename)
        {
            return fromFilename == null ? _highLevelContents : _highLevelContents.Where(h => h.IsPublic || h.Filename == fromFilename);
        }

        public void AddHighLevelContent(IHighLevelContent content)
        {
            _highLevelContents.Add(content);
        }

        public string FileReader(string filename)
        {
            return _fileReader(filename);
        }

        public void FileWriter(string filename, string contents)
        {
            _fileWriter(filename, contents);
        }

        public void EmitLine(int indentLevel, string s)
        {
            if (indentLevel > 0)
                _emitted.Append('\t', indentLevel);
            _emitted.Append(s);
            _emitted.Append(Environment.NewLine);
        }

        public string EmittedText
        {
            get { return _emitted.ToString(); }
        }

        public List<CompilerError> Errors
        {
            get { return _errors; }
        }

        public void AddError(CharStream.Mark mark, string description)
        {
            _errors.Add(new CompilerError(mark, description));
        }

        public string GenerateLabel()
        {
            return "__label" + _currentFreeLabel++;
        }
    }
}