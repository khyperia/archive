using System;
using System.Collections.Generic;
using System.Linq;
using ProgComC.Parser;

namespace ProgComC.HighLevelContent
{
    public static class Compiler
    {
        public static string[] Compile(string initialFile, Dictionary<string, string> flags, Func<string, string> fileReader, Action<string, string> fileWriter)
        {
            var context = new CompilerContext(fileReader, fileWriter);
            Parse(initialFile, context);
            if (context.Errors.Count != 0)
                return PrintErrors(context);
            var compiler = GetCompiler("progcom");
            if (compiler == null)
                return new[] { "Unknown target assembly" };
            compiler.Visit(context);
            return context.Errors.Count != 0 ? PrintErrors(context) : null;
        }

        internal static void Parse(string file, CompilerContext context)
        {
            if (context.ParsedFiles.Contains(file))
                return;
            context.ParsedFiles.Add(file);
            var textContents = context.FileReader(file);
            if (textContents == null)
                return;
            textContents = Preprocessor.Process(context, file, textContents);
            var stream = new CharStream(file, textContents);
            stream.TakeWhitespace();
            var content = HighLevelContent.Parse(stream);
            if (stream.HasRemaining())
                stream.Error("Expected end of file", false, null);
            if (stream.Errors.Count > 0)
                context.Errors.AddRange(stream.Errors);
            foreach (var item in content)
                context.AddHighLevelContent(item);
        }

        private static IHighLevelContentVisitor<CompilerContext> GetCompiler(string compiler)
        {
            compiler = compiler ?? "progcom";
            compiler = compiler.ToLower();
            switch (compiler)
            {
                case "progcom":
                    return ProgComBackend.ProgComHighLevelContentVisitor.Fetch;
                default:
                    return null;
            }
        }

        private static string[] PrintErrors(CompilerContext context)
        {
            return context.Errors.Select(error => string.Format("{0} - line {1}, column {2}: {3}", error.File, error.Line,
                                               error.Column, error.Description)).ToArray();
        }
    }
}
