using ProgComC.Expressions.BasicValue;
using ProgComC.HighLevelContent;

namespace ProgComC.TypeIdentifier
{
    class PointerTypeIdentifier : ITypeIdentifier
    {
        private readonly ITypeIdentifier _pointerTo;

        public PointerTypeIdentifier(ITypeIdentifier pointerTo)
        {
            _pointerTo = pointerTo;
        }

        public ITypeIdentifier PointerTo
        {
            get { return _pointerTo; }
        }

        public int Size(CompilerContext context)
        {
            return 1;
        }

        public bool IsRegisterLiteral()
        {
            return true;
        }

        public Identifier VariableName
        {
            get { return _pointerTo.VariableName; }
        }

        public bool Equals(ITypeIdentifier o)
        {
            var bti = o as PointerTypeIdentifier;
            return bti != null && bti.PointerTo.Equals(PointerTo);
        }

        public override string ToString()
        {
            return PointerTo + "*";
        }
    }
}