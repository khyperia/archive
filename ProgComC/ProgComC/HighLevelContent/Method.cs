using System.Collections.Generic;
using System.Linq;
using ProgComC.Expressions.BasicValue;
using ProgComC.Parser;
using ProgComC.Statement;
using ProgComC.TypeIdentifier;

namespace ProgComC.HighLevelContent
{
    class Method : IHighLevelContent
    {
        private readonly CharStream.Mark _mark;
        private readonly bool _isExtern;
        private readonly ITypeIdentifier _returnType;
        private readonly Identifier _methodName;
        private readonly List<KeyValuePair<ITypeIdentifier, Identifier>> _parameters;
        private readonly ILine _contents;

        public void Accept<T>(IHighLevelContentVisitor<T> visitor, T data)
        {
            visitor.Visit(this, data);
        }

        public string Filename { get; private set; }
        public bool IsPublic { get; private set; }

        public Method(CharStream.Mark mark, string filename, bool isPublic, bool isExtern, ITypeIdentifier returnValue, Identifier methodName, List<KeyValuePair<ITypeIdentifier, Identifier>> parameters, ILine contents)
        {
            _mark = mark;
            _isExtern = isExtern;
            _returnType = returnValue;
            _methodName = methodName;
            _parameters = parameters;
            _contents = contents;
            Filename = filename;
            IsPublic = isPublic;
        }

        public static Method Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            var isPublic = stream.TakeIf("public", "private") == "public";
            stream.TakeWhitespace();
            var isExtern = stream.TakeIf("extern");
            stream.TakeWhitespace();
            var returnValue = TypeIdentifierParser.Parse(stream);
            if (returnValue == null)
            { stream.ResetPosition(marked); return null; }
            var methodName = Identifier.Parse(stream);
            if (methodName == null || stream.TakeIf('(') == false)
            { stream.ResetPosition(marked); return null; }
            stream.TakeWhitespace();
            var parameters = new List<KeyValuePair<ITypeIdentifier, Identifier>>();
            while (true)
            {
                var typeId = TypeIdentifierParser.Parse(stream);
                if (typeId == null)
                {
                    if (parameters.Count != 0)
                        stream.Error("Expected type", false, ")", ";", "}");
                    break;
                }
                var name = typeId.VariableName ?? Identifier.Parse(stream);
                if (name == null)
                    stream.Error("Expected identifier", false, ")", ";", "}");
                parameters.Add(new KeyValuePair<ITypeIdentifier, Identifier>(typeId, name));
                if (stream.TakeIf(',') == false)
                    break;
                stream.TakeWhitespace();
            }
            if (stream.TakeIf(')') == false)
            {
                stream.Error("Expected closing parentheses", false, ";", "}");
                return null;
            }
            stream.TakeWhitespace();
            var contents = Line.Parse(stream);
            if (contents == null)
            {
                if (stream.TakeIf(';') == false)
                    stream.Error("Expected method body or semicolon", false, null);
                stream.TakeWhitespace();
            }
            return new Method(marked, stream.Filename, isPublic, isExtern, returnValue, methodName, parameters, contents);
        }

        public ILine MethodBlock
        {
            get { return _contents; }
        }

        public List<KeyValuePair<ITypeIdentifier, Identifier>> Parameters
        {
            get { return _parameters; }
        }

        public ITypeIdentifier ReturnType
        {
            get { return _returnType; }
        }

        private StructDefinition _methodStruct;

        public StructDefinition MethodStruct
        {
            get
            {
                if (_methodStruct == null)
                {
                    var localDefinitions = new List<VariableDeclarationStatement>();
                    if (_contents != null)
                        _contents.Traverse<VariableDeclarationStatement>(localDefinitions.Add);
                    var localTypes = new[] { _returnType, new IntTypeIdentifier() }.Concat(_parameters.Select(t => t.Key)).Concat(localDefinitions.Select(l => l.Type)).ToArray();
                    var localNames = new[] { "%retval", "%retptr" }.Concat(_parameters.Select(t => t.Value.Name)).Concat(localDefinitions.Select(l => l.Variable.Name)).ToArray();
                    _methodStruct = new StructDefinition(localTypes, localNames);
                }
                return _methodStruct;
            }
        }

        public CharStream.Mark Mark
        {
            get { return _mark; }
        }

        public Identifier MethodName
        {
            get { return _methodName; }
        }

        public bool IsExtern
        {
            get { return _isExtern; }
        }
    }
}