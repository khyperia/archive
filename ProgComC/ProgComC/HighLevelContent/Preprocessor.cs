using System.Linq;
using System.Text.RegularExpressions;
using ProgComC.Parser;

namespace ProgComC.HighLevelContent
{
    static class Preprocessor
    {
        private static readonly Regex PreprocessorRegex = new Regex(@" *#([^\r\n]*)");

        public static string Process(CompilerContext context, string file, string source)
        {
            foreach (Match match in PreprocessorRegex.Matches(source))
            {
                var preprocessor = match.Groups[1].Value;
                var split = preprocessor.Split(' ');
                switch (split[0])
                {
                    case "include":
                        if (split.Length == 1)
                        {
                            context.AddError(new CharStream.Mark(file, source, match.Index), "No filename argument supplied to #include directive");
                            break;
                        }
                        var filename = split.Skip(1).Aggregate((total, part) => total + " " + part);
                        Compiler.Parse(filename, context);
                        break;
                    case "asminclude":
                        if (split.Length == 1)
                        {
                            context.AddError(new CharStream.Mark(file, source, match.Index), "No filename argument supplied to #asminclude directive");
                            break;
                        }
                        var asmfilename = split.Skip(1).Aggregate((total, part) => total + " " + part);
                        context.AddHighLevelContent(new AsmInclude(asmfilename));
                        break;
                    default:
                        context.AddError(new CharStream.Mark(file, source, match.Index), "Unknown preprocessor directive '" + split[0] + "'");
                        break;
                }
            }
            return PreprocessorRegex.Replace(source, "");
        }
    }
}
