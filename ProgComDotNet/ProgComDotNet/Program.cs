using System;
using ProgComDotNet.ProgcomIl;

namespace ProgComDotNet
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var type = typeof(DerpTest);
                foreach (var instruction in CilToProgcomConverter.ConvertMethod(type.GetMethod("main")))
                {
                    Console.WriteLine(instruction);
                }
            }
            catch (CompilerException e)
            {
                Console.WriteLine("Internal compiler error: " + e.Message);
            }
            Console.ReadKey(true);
        }
    }

    internal class CompilerException : Exception
    {
        public CompilerException(string message)
            : base(message)
        {
        }
    }

    class LabelAttribute : Attribute
    {
    }

    class DerpTest
    {
        private static int[] pcmem;

        [Label]
        private static readonly int GLOBAL_NUMPAD_MSG;

        private static int blah;

        public static void main()
        {
            var x = 2;
            blah = x + 2;
            while (x < 2)
            {
                x++;
            }
            blah = Foo(x, blah);
        }

        private static int Foo(int x, int y)
        {
            x *= GLOBAL_NUMPAD_MSG;
            return x * y;
        }
    }
}
