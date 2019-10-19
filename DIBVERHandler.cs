using System.Collections.Generic;
using System.IO;

namespace DibClient
{
    static class DIBVERHandler
    {
        const char SEPARATOR = ':';

        public static Dictionary<string, int> GetVersionsData(string path)
        {
            Dictionary<string, int> output = new Dictionary<string, int>();

            if (!File.Exists(path))
                return output;

            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] components = line.Split(SEPARATOR);
                if (components.Length != 2)
                {
                    System.Console.WriteLine($"DIBVERHandler: Warning: Cannot parse line {line}");
                    continue;
                }

                string appName = components[0];
                if (output.ContainsKey(appName))
                {
                    System.Console.WriteLine($"DIBVERHandler: Warning: app {appName} defined more than once");
                    continue;
                }

                int.TryParse(components[1], out int version);
                output.Add(appName, version);
            }

            return output;
        }

        public static void SaveVersionsData(string path, Dictionary<string, int> versionsData)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, "");

            List<string> entries = new List<string>();

            foreach (KeyValuePair<string, int> pair in versionsData)
                entries.Add($"{pair.Key}{SEPARATOR}{pair.Value}");

            File.WriteAllLines(path, entries);
        }
    }
}