using System.Collections.Generic;
using System.IO;

namespace DibClient
{
    public static class DIBFHandler
    {

        public static void AddFileToRepo(string repoPath, string fileRelativePath)
        {
            if (!File.Exists($"{repoPath}/{Paths.DIBF_FILE_RELATIVE_PATH}"))
                return;

            string[] lines = File.ReadAllLines($"{repoPath}/{Paths.DIBF_FILE_RELATIVE_PATH}");
            foreach (string line in lines)
                if (line.Trim() == fileRelativePath)
                    return;

            File.AppendAllText($"{repoPath}/{Paths.DIBF_FILE_RELATIVE_PATH}", $"{fileRelativePath}\n");
        }

        public static void RemoveFileFromRepo(string repoPath, string fileRelativePath)
        {
            if (!File.Exists($"{repoPath}/{Paths.DIBF_FILE_RELATIVE_PATH}"))
                return;

            List<string> linesToSave = new List<string>();
            string[] currentLines = File.ReadAllLines($"{repoPath}/{Paths.DIBF_FILE_RELATIVE_PATH}");
            foreach (string line in currentLines)
                if (line.Trim() != fileRelativePath)
                    linesToSave.Add(line);

            File.WriteAllLines($"{repoPath}/{Paths.DIBF_FILE_RELATIVE_PATH}", linesToSave);
        }
    }
}
