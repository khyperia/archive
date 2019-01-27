using System;

namespace ProgComC.Parser
{
    static class AstNodeHelper
    {
        public static void Traverse<T>(this IAstNode node, Action<T> visit) where T : IAstNode
        {
            if (node is T)
                visit((T)node);
            foreach (var content in node.Contents)
                content.Traverse(visit);
        }
    }
}