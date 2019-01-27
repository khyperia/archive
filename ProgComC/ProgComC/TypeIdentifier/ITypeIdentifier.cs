using ProgComC.Expressions.BasicValue;
using ProgComC.HighLevelContent;

namespace ProgComC.TypeIdentifier
{
    interface ITypeIdentifier
    {
        int Size(CompilerContext context);
        bool IsRegisterLiteral();
        bool Equals(ITypeIdentifier o);
        Identifier VariableName { get; }
    }
}