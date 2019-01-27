using ProgComC.Parser;

namespace ProgComC.Expressions
{
    internal interface IValue : IAstNode
    {
        T1 Accept<T1, T2>(IValueVisitor<T1, T2> visitor, T2 data);
        object ConstantFold();
    }
}