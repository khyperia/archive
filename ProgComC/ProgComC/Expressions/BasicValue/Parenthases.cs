using ProgComC.Expressions.Operation;
using ProgComC.Parser;

namespace ProgComC.Expressions.BasicValue
{
    internal static class Parentheses
    {
        public static IValue Parse(CharStream stream)
        {
            if (stream.TakeIf('(') == false)
                return null;
            stream.TakeWhitespace();
            var value = Operators.Parse(stream);
            if (stream.TakeIf(')') == false)
                stream.Error("Expected closing parentheses", true, ")", ";", "}");
            stream.TakeWhitespace();
            return value;
        }
    }
}