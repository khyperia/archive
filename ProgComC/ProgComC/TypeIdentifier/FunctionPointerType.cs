using System.Collections.Generic;
using System.Linq;
using ProgComC.Expressions.BasicValue;
using ProgComC.HighLevelContent;
using ProgComC.Parser;

namespace ProgComC.TypeIdentifier
{
    class FunctionPointerType : ITypeIdentifier
    {
        private readonly CharStream.Mark _mark;
        private readonly ITypeIdentifier _returnType;
        private readonly Identifier _variableName;
        private readonly ITypeIdentifier[] _parameters;
        private Method _method;

        public FunctionPointerType(CharStream.Mark mark, ITypeIdentifier returnType, Identifier variableName, ITypeIdentifier[] parameters)
        {
            _mark = mark;
            _returnType = returnType;
            _variableName = variableName;
            _parameters = parameters;
        }

        public Identifier VariableName
        {
            get { return _variableName; }
        }

        public int Size(CompilerContext context)
        {
            return 1;
        }

        public bool IsRegisterLiteral()
        {
            return true;
        }

        public bool Equals(ITypeIdentifier o)
        {
            var fpt = o as FunctionPointerType;
            return fpt != null && fpt._parameters.Length == _parameters.Length && fpt._returnType.Equals(_returnType) &&
                   fpt._parameters.Select((t, i) => _parameters[i].Equals(t)).All(t => t);
        }

        public Method Method
        {
            get
            {
                if (_method == null)
                {
                    var i = 0;
                    var parameters = _parameters.Select(parameter => new KeyValuePair<ITypeIdentifier, Identifier>(parameter, new Identifier(_mark, "arg" + i++))).ToList();
                    _method = new Method(_mark, null, false, true, _returnType, null, parameters, null);
                }
                return _method;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} (*{1})({2})", _returnType, _variableName.Source(), string.Join(", ", _parameters.Select(p => p.ToString()).ToArray()));
        }
    }
}