using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    class DiceException : Exception
    {
        public DiceException(string message) : base(message)
        {
        }
    }

    private static readonly Random rand = new Random();
    // okay I must admit, sometimes Python not having to specify types is kind of nice
    private static readonly Dictionary<char, KeyValuePair<int, Func<double, double, double>>> operators = new Dictionary<char, KeyValuePair<int, Func<double, double, double>>>
    {
        { '+', new KeyValuePair<int, Func<double, double, double>>(1, (x, y) => x + y) },
        { '-', new KeyValuePair<int, Func<double, double, double>>(1, (x, y) => x - y) },
        { '*', new KeyValuePair<int, Func<double, double, double>>(1, (x, y) => x * y) },
        { '/', new KeyValuePair<int, Func<double, double, double>>(1, (x, y) => x / y) },
        { '%', new KeyValuePair<int, Func<double, double, double>>(1, (x, y) => x % y) },
        { '^', new KeyValuePair<int, Func<double, double, double>>(1, Math.Pow) },
        { 'd', new KeyValuePair<int, Func<double, double, double>>(1, (x, y) => Roll(x, y)) },
    };

    private static double Roll(double x, double y)
    {
        if (x > 1000)
        {
            throw new DiceException("Nobody has that many dice!");
        }
        return Enumerable.Range(0, (int)x).Sum(a => Math.Floor(rand.NextDouble() * Math.Floor(y)) + 1.0);
    }

    static double Primary(string str, ref int index)
    {
        if (index < str.Length && str[index] == '(')
        {
            index++;
            var prim_parse = Parse(str, Primary(str, ref index), 0, ref index);
            if (index >= str.Length || str[index] != ')')
            {
                throw new DiceException("Missing ')'");
            }
            index++;
            return prim_parse;
        }
        var start_index = index;
        while (index < str.Length && char.IsDigit(str, index))
        {
            index++;
        }
        if (index == start_index)
        {
            throw new DiceException("No digit where there should have been.");
        }
        return double.Parse(str.Substring(start_index, index - start_index));
    }

    private static double Parse(string str, double lhs, int min_precedence, ref int index)
    {
        KeyValuePair<int, Func<double, double, double>> rule;
        while (index < str.Length && operators.TryGetValue(str[index], out rule) && rule.Key >= min_precedence)
        {
            var oldrule = rule;
            var op = str[index];
            index++;
            var rhs = Primary(str, ref index);
            while (index < str.Length && operators.TryGetValue(str[index], out rule) && rule.Key > oldrule.Key)
            {
                rhs = Parse(str, rhs, rule.Key, ref index);
            }
            lhs = oldrule.Value(lhs, rhs);
        }
        return lhs;
    }

    private static double CalculateExpr(string str)
    {
        var index = 0;
        var result = Parse(str, Primary(str, ref index), 0, ref index);
        if (index != str.Length)
        {
            throw new DiceException("Couldn't parse string");
        }
        return result;
    }

    public static string Roll(string str)
    {
        try
        {
            int onlyNum;
            if (int.TryParse(str, out onlyNum) && onlyNum > 0)
            {
                return "Your roll is: " + Roll(1, onlyNum);
            }
            return "Your roll is: " + CalculateExpr(str);
        }
        catch (DiceException ex)
        {
            return "Uh oh! " + ex.Message;
        }
    }

    static void Main()
    {
        while (true)
        {
            var str = Console.ReadLine();
            if (str == null)
            {
                break;
            }
            Console.WriteLine(GeneralRoll(str));
        }
    }
}
