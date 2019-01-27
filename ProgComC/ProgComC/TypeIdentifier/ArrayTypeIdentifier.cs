using ProgComC.Expressions.BasicValue;
using ProgComC.HighLevelContent;

namespace ProgComC.TypeIdentifier
{
    class ArrayTypeIdentifier : ITypeIdentifier
    {
        private readonly ITypeIdentifier _elementType;
        private readonly int _size;

        public ArrayTypeIdentifier(ITypeIdentifier elementType, int size)
        {
            _elementType = elementType;
            _size = size;
        }

        public int Size(CompilerContext context)
        {
            return _elementType.Size(context)*_size;
        }

        public bool IsRegisterLiteral()
        {
            return false;
        }

        public bool Equals(ITypeIdentifier o)
        {
            var ati = o as ArrayTypeIdentifier;
            return ati != null && ati._size == _size && ati._elementType == _elementType;
        }

        public Identifier VariableName { get { return null; } }

        public ITypeIdentifier ElementType
        {
            get { return _elementType; }
        }
    }
}
