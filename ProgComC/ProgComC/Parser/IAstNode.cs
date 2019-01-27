using System.Collections.Generic;

namespace ProgComC.Parser
{
    interface IAstNode
    {
        IEnumerable<IAstNode> Contents { get; }
        CharStream.Mark Mark { get; }
        string Source();
    }
}