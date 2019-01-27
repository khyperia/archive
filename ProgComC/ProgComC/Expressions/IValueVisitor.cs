using ProgComC.Expressions.BasicValue;
using ProgComC.Expressions.Operation;

namespace ProgComC.Expressions
{
    internal interface IValueVisitor<out T1, in T2>
    {
        T1 Visit(BooleanLiteral node, T2 data);
        T1 Visit(Identifier node, T2 data);
        T1 Visit(IntegerLiteral node, T2 data);
        T1 Visit(NullLiteral node, T2 data);
        T1 Visit(SizeofOperator node, T2 data);
        T1 Visit(SpecialIdentifier node, T2 data);
        T1 Visit(AddressOfOperation node, T2 data);
        T1 Visit(AssignmentOperation node, T2 data);
        T1 Visit(BinaryOperation node, T2 data);
        T1 Visit(CastOperation node, T2 data);
        T1 Visit(DereferenceOperation node, T2 data);
        T1 Visit(DotOperation node, T2 data);
        T1 Visit(EqualityOperation node, T2 data);
        T1 Visit(GreaterThanOperation node, T2 data);
        T1 Visit(GreaterThanOrEqualOperation node, T2 data);
        T1 Visit(IndexerOperation node, T2 data);
        T1 Visit(InequalityOperation node, T2 data);
        T1 Visit(LessThanOperation node, T2 data);
        T1 Visit(LessThanOrEqualOperation node, T2 data);
        T1 Visit(MethodCall node, T2 data);
        T1 Visit(NegationOperation node, T2 data);
        T1 Visit(NotOperation node, T2 data);
        T1 Visit(StringLiteral node, T2 data);
        T1 Visit(PreIncOperation node, T2 data);
        T1 Visit(PostIncOperation node, T2 data);
        T1 Visit(PreDecOperation node, T2 data);
        T1 Visit(PostDecOperation node, T2 data);
        T1 Visit(FloatLiteral node, T2 data);
        T1 Visit(BitwiseInversionOperation node, T2 data);
        T1 Visit(ModOperation node, T2 data);
    }
}