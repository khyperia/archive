using System.Linq;
using ProgComC.Expressions.BasicValue;
using ProgComC.HighLevelContent;
using ProgComC.Parser;

namespace ProgComC.TypeIdentifier
{
    class CustomTypeIdentifier : ITypeIdentifier
    {
        private readonly CharStream.Mark _mark;
        private readonly Identifier _name;
        private bool _resolvedStruct;
        private StructDefinition _structDefinition;

        public CustomTypeIdentifier(Identifier name)
        {
            _name = name;
            _mark = name.Mark;
        }

        public int Size(CompilerContext context)
        {
            ResolveStruct(context);
            if (_structDefinition != null)
                return _structDefinition.Size(context);

            context.AddError(_mark, "Unknown type '" + _name.Name + "'");
            return -1;
        }

        public bool IsRegisterLiteral()
        {
            return false;
        }

        public StructDefinition ResolveStruct(CompilerContext context)
        {
            if (_resolvedStruct == false)
            {
                _resolvedStruct = true;
                _structDefinition = context.AccessableHighLevelContents(_mark.File).OfType<StructDefinition>().LastOrDefault(s => s.Name == _name.Name);
            }
            return _structDefinition;
        }

        public Identifier VariableName
        {
            get { return null; }
        }

        public bool Equals(ITypeIdentifier o)
        {
            var bti = o as CustomTypeIdentifier;
            return bti != null && bti._name.Name == _name.Name;
        }

        public override string ToString()
        {
            return _name.Name;
        }
    }
}