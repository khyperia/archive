using System;
using System.Collections.Generic;
using System.Linq;
using ProgComC.Expressions.BasicValue;
using ProgComC.Parser;
using ProgComC.TypeIdentifier;

namespace ProgComC.HighLevelContent
{
    class StructDefinition : IHighLevelContent
    {
        private readonly string _name;
        private bool _resolved;
        private readonly ITypeIdentifier[] _types;
        private readonly string[] _names;
        private readonly int[] _offsets;
        private int _size;

        public string Filename { get; private set; }
        public bool IsPublic { get; private set; }

        public StructDefinition(ITypeIdentifier[] types, string[] names)
        {
            if (types.Length != names.Length)
                throw new Exception("Internal exception: StructDefintion types/names not same length");
            _types = types;
            _names = names;
            _offsets = new int[types.Length];
        }

        private StructDefinition(ITypeIdentifier[] types, string[] names, string structName, bool isPublic, string filename)
            : this(types, names)
        {
            _name = structName;
            Filename = filename;
            IsPublic = isPublic;
        }

        public static StructDefinition Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            var isPublic = stream.TakeIf("public", "private") == "public";
            if (stream.TakeIf("struct") == false)
            {
                stream.ResetPosition(marked);
                return null;
            }
            stream.TakeWhitespace();
            var name = Identifier.Parse(stream);
            if (name == null)
                stream.Error("Expected identifier", true, " ", "\n", "\r");
            if (stream.TakeIf('{') == false)
                stream.Error("Expected opening brace", true, "{", "}");
            stream.TakeWhitespace();
            var fields = new Dictionary<string, ITypeIdentifier>();
            while (true)
            {
                var fieldTypeid = TypeIdentifierParser.Parse(stream);
                if (fieldTypeid == null)
                    break;
                var fieldName = Identifier.Parse(stream);
                if (fieldName == null || stream.TakeIf(';') == false)
                    stream.Error("Expected semicolon", false, ";", "}");
                stream.TakeWhitespace();
                if (fieldName == null)
                    continue;
                if (fields.ContainsKey(fieldName.Name))
                    stream.Error("Struct definition contains duplicate field name " + fieldName.Name, false, null);
                fields.Add(fieldName.Name, fieldTypeid);
            }
            if (stream.TakeIf('}') == false)
                stream.Error("Expected closing brace", true, ";", "}");
            stream.TakeWhitespace();
            if (stream.TakeIf(';'))
                stream.TakeWhitespace();
            return new StructDefinition(fields.Values.ToArray(), fields.Keys.ToArray(), name == null ? null : name.Name, isPublic, stream.Filename);
        }

        public void Accept<T>(IHighLevelContentVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        private void Resolve(CompilerContext context)
        {
            _resolved = true;
            var offset = 0;
            for (var i = 0; i < _offsets.Length; i++)
            {
                _offsets[i] = offset;
                offset += _types[i].Size(context);
            }
            _size = offset;
        }

        public int Size(CompilerContext context)
        {
            if (_resolved == false)
                Resolve(context);
            return _size;
        }

        public int OffsetOf(CompilerContext context, string variableName)
        {
            if (_resolved == false)
                Resolve(context);
            for (var i = 0; i < _names.Length; i++)
                if (_names[i] == variableName)
                    return _offsets[i];
            return -1;
        }

        public ITypeIdentifier TypeOf(CompilerContext context, string variableName)
        {
            if (_resolved == false)
                Resolve(context);
            for (var i = 0; i < _names.Length; i++)
                if (_names[i] == variableName)
                    return _types[i];
            return null;
        }

        public string Name
        {
            get { return _name; }
        }

        public string[] DuplicateFields()
        {
            return _names.GroupBy(s => s).Where(s => s.Count() > 1).Select(s => s.Key).ToArray();
        }
    }
}