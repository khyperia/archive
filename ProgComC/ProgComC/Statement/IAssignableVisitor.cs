using ProgComC.Expressions.BasicValue;
using ProgComC.Expressions.Operation;

namespace ProgComC.Statement
{
    internal interface IAssignableVisitor<in T1, in T2>
    {
        void Visit(DereferenceOperation node, T1 assignedValue, T2 data);
        void Visit(DotOperation node, T1 assignedValue, T2 data);
        void Visit(Identifier node, T1 assignedValue, T2 data);
        void Visit(IndexerOperation node, T1 assignedValue, T2 data);
        void Visit(SpecialIdentifier node, T1 assignedValue, T2 data);
    }
}