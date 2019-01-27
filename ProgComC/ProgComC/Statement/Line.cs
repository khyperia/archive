using ProgComC.Expressions.Operation;
using ProgComC.Parser;

namespace ProgComC.Statement
{
    static class Line
    {
        public static ILine Parse(CharStream stream)
        {
            var marked = stream.MarkPosition();
            if (stream.TakeIf('{'))
            {
                stream.TakeWhitespace();
                var block = Block.Parse(stream);
                if (stream.TakeIf('}') == false)
                    stream.Error("Expected closing brace", true, "}");
                stream.TakeWhitespace();
                return block;
            }
            if (stream.TakeIf("if"))
            {
                stream.TakeWhitespace();
                return IfStatement.Parse(stream);
            }
            if (stream.TakeIf("while"))
            {
                stream.TakeWhitespace();
                return WhileStatement.Parse(stream);
            }
            if (stream.TakeIf("for"))
            {
                stream.TakeWhitespace();
                return ForStatement.Parse(stream);
            }
            if (stream.TakeIf("return"))
            {
                stream.TakeWhitespace();
                return ReturnStatement.Parse(stream);
            }
            if (stream.TakeIf("break"))
            {
                stream.TakeWhitespace();
                if (stream.TakeIf(';') == false)
                    stream.Error("Expected semicolon", true, ";");
                stream.TakeWhitespace();
                return new BreakStatement(marked);
            }
            if (stream.TakeIf("asm"))
            {
                stream.TakeWhitespace();
                return InlineAsmStatement.Parse(stream);
            }
            if (stream.TakeIf("continue"))
            {
                stream.TakeWhitespace();
                if (stream.TakeIf(';') == false)
                    stream.Error("Expected semicolon", true, ";");
                stream.TakeWhitespace();
                return new ContinueStatement(marked);
            }
            var vdc = VariableDeclarationStatement.Parse(stream);
            if (vdc != null)
                return vdc;
            var value = Operators.Parse(stream);
            if (value != null)
            {
                if (stream.TakeIf(';') == false)
                    stream.Error("Expected semicolon", true, ";");
                stream.TakeWhitespace();
                var line = value as ILine;
                if (line == null)
                {
                    stream.Error("Line is not a valid statement", false, null);
                    line = new VoidStatement(marked);
                }
                return line;
            }
            if (stream.TakeIf(';'))
            {
                stream.TakeWhitespace();
                return new VoidStatement(marked);
            }
            return null;
        }
    }
}
