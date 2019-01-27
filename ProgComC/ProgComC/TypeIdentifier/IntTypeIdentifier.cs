using ProgComC.Expressions.BasicValue;
using ProgComC.HighLevelContent;

namespace ProgComC.TypeIdentifier
{
    class IntTypeIdentifier : ITypeIdentifier
    {
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
            get { return null; }
        }

        public bool Equals(ITypeIdentifier o)
        {
            return o is IntTypeIdentifier;
        }

        public override string ToString()
        {
            return "int";
        }
    }
}