using System.Collections.Generic;
using System.IO;

namespace DibClient
{
    static class DIBVERHandler
    {
        const char SEPARATOR = ':';

        public static Dictionary<string, int> GetVersions(string path)
        {
            Dictionary<string, int> output = new Dictionary<string, int>();

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

        public static void saveVersions(string path, Dictionary<string, int> versionsData)
        {
            List<string> entries = new List<string>();

            foreach (KeyValuePair<string, int> pair in versionsData)
                entries.Add($"{pair.Key}{SEPARATOR}{pair.Value}");

            File.WriteAllLines(path, entries);
        }
    }
}