using System;
using System.Collections.Generic;
using System.Text;

public class Program
{
    private static Random random = new Random();

    private static Dictionary<string, List<string>> ReadDatabase()
    {
        var database = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string line;
        long count = 0;
        while ((line = Console.ReadLine()) != null)
        {
            var split = line.Split(' ');
            for (var i = 0; i < split.Length; i++)
            {
                var key = split[i];
                string value;
                if (i == split.Length - 1)
                {
                    if (random.Next(2) != 0)
                    {
                        continue;
                    }
                    value = null;
                }
                else
                {
                    value = split[i + 1];
                }
                if (!database.TryGetValue(key, out var entry))
                {
                    database.Add(key, entry = new List<string>());
                }
                entry.Add(value);
            }
            if (count % 2500 == 0)
                Console.WriteLine(count);
            count++;
        }
        return database;
    }

    private static string RandomChoice(IReadOnlyList<string> list)
    {
        return list[random.Next(list.Count)];
    }

    private static string Generate(List<string> keys, Dictionary<string, List<string>> database, StringBuilder sentence)
    {
        var word = RandomChoice(keys);
        sentence.Clear();
        sentence.Append(word);
        do
        {
            if (!database.TryGetValue(word, out var entry))
                break;
            word = RandomChoice(entry);
            if (word == null)
                break;
            sentence.Append(' ').Append(word);
        } while (true);
        return sentence.ToString();
    }

    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: bigramGenerator.exe [count] < datafile.txt");
            return;
        }
        var count = int.Parse(args[0]);
        var database = ReadDatabase();
        var keys = new List<string>(database.Keys);
        var stringbuilder = new StringBuilder();
        Console.WriteLine("Done reading database");
        for (var i = 0; i < count; i++)
        {
            Console.WriteLine(Generate(keys, database, stringbuilder));
        }
    }
}
