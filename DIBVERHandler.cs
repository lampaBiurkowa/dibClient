using EncodingMaster;
using System.Collections.Generic;
using System.IO;

namespace DibClient
{
    static class DIBVERHandler
    {
        const char SEPARATOR = ':';
        const string ENCODING_CONF_FILE_PATH = "encoding.conf";

        public static Dictionary<string, int> GetVersionsData(string path)
        {
            Dictionary<string, int> output = new Dictionary<string, int>();

            if (!File.Exists(path))
                return output;

            string encodedContent = File.ReadAllText(path);
            string decodedContent = Decoder.Decode(encodedContent, getEncodingPassword(), getEncodingParameter());

            string[] lines = decodedContent.Split('\n');
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

        static string getEncodingPassword()
        {
            const int PASSWORD_LINE_INDEX = 0;

            string[] lines = File.ReadAllLines(ENCODING_CONF_FILE_PATH);
            return lines[PASSWORD_LINE_INDEX];
        }

        static int getEncodingParameter()
        {
            const int PARAMETER_LINE_INDEX = 1;

            string[] lines = File.ReadAllLines(ENCODING_CONF_FILE_PATH);
            return int.Parse(lines[PARAMETER_LINE_INDEX]);
        }

        public static void SaveVersionsData(string path, Dictionary<string, int> versionsData)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, "");

            string content = "";

            foreach (KeyValuePair<string, int> pair in versionsData)
               content += $"{pair.Key}{SEPARATOR}{pair.Value}\n";

            System.Console.WriteLine($"fdsDFSADFSFDSA {content}");
            string encodedContent = Encoder.Encode(content, getEncodingPassword(), getEncodingParameter());
            System.Console.WriteLine($"fdsDFSADFSFDSA {encodedContent}");

            File.WriteAllText(path, encodedContent);
        }
    }
}