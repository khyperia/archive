using ProgComC.Expressions.BasicValue;
using ProgComC.HighLevelContent;

namespace ProgComC.TypeIdentifier
{
    class VoidTypeIdentifier : ITypeIdentifier
    {
        public int Size(CompilerContext context)
        {
            return 0;
        }

        public bool IsRegisterLiteral()
        {
            return false;
        }

        public bool Equals(ITypeIdentifier o)
        {
            return o is VoidTypeIdentifier;
        }

        public Identifier VariableName
        {
            get { return null; }
        }

        public override string ToString()
        {
            return "void";
        }
    }
}