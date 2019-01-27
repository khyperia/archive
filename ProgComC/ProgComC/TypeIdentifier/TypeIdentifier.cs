using System.Collections.Generic;
using ProgComC.Expressions.BasicValue;
using ProgComC.Parser;

namespace ProgComC.TypeIdentifier
{
    static class TypeIdentifierParser
    {
        public static ITypeIdentifier Parse(CharStream stream)
        {
            var identifier = Identifier.Parse(stream);
            if (identifier == null)
                return null;
            var typeId = identifier.Name == "void"
                             ? new VoidTypeIdentifier()
                             : identifier.Name == "int"
                                   ? new IntTypeIdentifier()
                                   : (ITypeIdentifier)new CustomTypeIdentifier(identifier);
            return ParsePost(stream, typeId);
        }

        private static ITypeIdentifier ParsePost(CharStream stream, ITypeIdentifier typeId)
        {
            while (stream.TakeIf('*'))
            {
                stream.TakeWhitespace();
                typeId = new PointerTypeIdentifier(typeId);
            }
            var fpt = ParseFpt(stream, typeId);
            if (fpt != null)
                typeId = fpt;
            return typeId;
        }

        private static ITypeIdentifier ParseFpt(CharStream stream, ITypeIdentifier returnType)
        {
            var marked = stream.MarkPosition();
            if (returnType == null || stream.TakeIf('(') == false)
            { stream.ResetPosition(marked); return null; }
            stream.TakeWhitespace();
            if (stream.TakeIf('*') == false)
            { stream.ResetPosition(marked); return null; }
            stream.TakeWhitespace();
            var variableName = Identifier.Parse(stream);
            if (variableName == null || stream.TakeIf(')') == false)
            { stream.ResetPosition(marked); return null; }
            stream.TakeWhitespace();
            if (stream.TakeIf('(') == false)
            { stream.ResetPosition(marked); return null; }
            stream.TakeWhitespace();
            var paramList = new List<ITypeIdentifier>();
            while (true)
            {
                var id = Parse(stream);
                if (id == null)
                {
                    if (paramList.Count == 0)
                        break;
                    stream.ResetPosition(marked); return null;
                }
                paramList.Add(id);
                if (stream.TakeIf(',') == false)
                    break;
                stream.TakeWhitespace();
            }
            if (stream.TakeIf(')') == false)
            { stream.ResetPosition(marked); return null; }
            stream.TakeWhitespace();
            var fpt = new FunctionPointerType(marked, returnType, variableName, paramList.ToArray());
            return ParsePost(stream, fpt);
        }
    }
}
