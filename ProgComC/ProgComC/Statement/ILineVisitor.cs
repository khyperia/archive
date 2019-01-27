using ProgComC.Expressions.Operation;

namespace ProgComC.Statement
{
    interface ILineVisitor<in T>
    {
        void Visit(Block node, T data);
        void Visit(BreakStatement node, T data);
        void Visit(ContinueStatement node, T data);
        void Visit(IfStatement node, T data);
        void Visit(InlineAsmStatement node, T data);
        void Visit(ReturnStatement node, T data);
        void Visit(VariableDeclarationStatement node, T data);
        void Visit(VoidStatement node, T data);
        void Visit(WhileStatement node, T data);
        void Visit(AssignmentOperation node, T data);
        void Visit(MethodCall node, T data);
        void Visit(PostDecOperation node, T data);
        void Visit(PreDecOperation node, T data);
        void Visit(PostIncOperation node, T data);
        void Visit(PreIncOperation node, T data);
        void Visit(ForStatement node, T data);
    }
}
