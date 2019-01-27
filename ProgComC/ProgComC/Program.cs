using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProgComC.HighLevelContent;

namespace ProgComC
{
    static class Program
    {
        static void Main(string[] args)
        {
            var flags = args.TakeWhile(s => s.StartsWith("/")).Select(s => s.Substring(1)).ToDictionary(
                s => s.Substring(0, s.Contains(":") ? s.IndexOf(':') : s.Length),
                s => s.Contains(":") ? s.Substring(s.IndexOf(':') + 1) : null);
            var filename = args.SkipWhile(s => s.StartsWith("/")).Aggregate("", (total, part) => total + " " + part).Substring(1);
            if (string.IsNullOrEmpty(filename))
            {
                if (File.Exists("program.c"))
                {
                    Console.WriteLine("No input file specified, compiling program.c");
                    filename = "program.c";
                }
                else
                {
                    Console.WriteLine("No input file specified, and the autorun file program.c is missing");
                    Console.ReadKey(true);
                    return;
                }
            }
            filename = Path.GetFullPath(filename);
            var currentDir = Path.GetDirectoryName(filename);
            if (currentDir != null)
                Environment.CurrentDirectory = currentDir;
            if (File.Exists(filename) == false)
            {
                Console.WriteLine("File \"" + filename + "\" not found");
                return;
            }
            if (flags.ContainsKey("debug"))
            {
                Compile(filename, flags);
            }
            else
            {
                try
                {
                    Compile(filename, flags);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.GetType().Name + ": " + e.Message);
                    Console.WriteLine("Spacebar to view stack trace, any other key to exit");
                    if (Console.ReadKey(true).Key == ConsoleKey.Spacebar)
                    {
                        Console.WriteLine(e);
                        Console.ReadKey(true);
                    }
                }
            }
        }

        static void Compile(string filename, Dictionary<string, string> flags)
        {
            var errors = Compiler.Compile(filename, flags, File.ReadAllText, File.WriteAllText);
            if (errors == null)
                return;
            foreach (var error in errors)
                Console.WriteLine(error);
            Console.ReadKey(true);
        }
    }
}
