using ProgComC.Parser;

namespace ProgComC.Expressions.BasicValue
{
    internal static class BasicValue
    {
        public static IValue Parse(CharStream stream)
        {
            return Parentheses.Parse(stream) ??
                   IntegerLiteral.Parse(stream) ??
                   BooleanLiteral.Parse(stream) ??
                   NullLiteral.Parse(stream) ??
                   SizeofOperator.Parse(stream) ??
                   SpecialIdentifier.Parse(stream) ??
                   Identifier.Parse(stream) ??
                   StringLiteral.Parse(stream);
        }
    }
}