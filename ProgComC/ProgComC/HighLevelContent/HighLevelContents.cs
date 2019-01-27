using System.Collections.Generic;
using ProgComC.Parser;

namespace ProgComC.HighLevelContent
{
    static class HighLevelContent
    {
        public static IHighLevelContent[] Parse(CharStream stream)
        {
            var list = new List<IHighLevelContent>();
            while (true)
            {
                var line = ParseSingle(stream);
                if (line == null)
                    return list.ToArray();
                list.Add(line);
            }
        }

        private static IHighLevelContent ParseSingle(CharStream stream)
        {
            var fieldDef = GlobalField.Parse(stream);
            if (fieldDef != null)
                return fieldDef;
            var structDef = StructDefinition.Parse(stream);
            if (structDef != null)
                return structDef;
            var method = Method.Parse(stream);
            return method;
        }
    }
}
