using ProgComC.Parser;

namespace ProgComC.Statement
{
    interface ILine : IAstNode
    {
        void Accept<T>(ILineVisitor<T> visitor, T data);
        bool Returns();
    }
}