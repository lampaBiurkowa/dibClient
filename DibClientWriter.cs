using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;

namespace DibClient
{
    public class DibClientWriter
    {
        const string EXCLUDED_DIBMDHANDLER_PATH = "DIBMDHandler.cpp";
        const string EXCLUDED_DIB_DIRECTORY_PATH = "dib.exe";
        const string EXCLUDED_DIB_EXE_PATH = "/*.dib";
        const string COMPRESSED_FILE_EXTENSION = ".zip";

        const int DEFAULT_PORT = 5000;

        static HttpClient httpClient = new HttpClient();

        public void Upload(string repoPath)
        {
            compressRepo(repoPath);

            string zipPath = getCompressedFilePath(repoPath);
            DIBMDHandler handler = new DIBMDHandler($"{repoPath}/{Paths.DIBMD_FILE_RELATIVE_PATH}");
            System.Console.WriteLine($"curl -F \"files=@{zipPath}\" http://{handler.GetRemoteAddress()}:{handler.GetPort()}/api/dibUpload");

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "curl";
            startInfo.Arguments = $" -F \"files=@{zipPath}\" http://{handler.GetRemoteAddress()}:{handler.GetPort()}/api/dibUpload";
            Process.Start(startInfo);
        }

        void compressRepo(string repoPath)
        {
            string zipFileName = getCompressedFilePath(repoPath);
            string[] paths = File.ReadAllLines($"{repoPath}/{Paths.DIBF_FILE_RELATIVE_PATH}");
            using (ZipArchive archive = ZipFile.Open(zipFileName, ZipArchiveMode.Update))
            {
                foreach (string path in paths)
                    archive.CreateEntryFromFile($"{repoPath}/{path}", path);

                archive.CreateEntryFromFile($"{repoPath}/{Paths.DIBMD_FILE_RELATIVE_PATH}", Paths.DIBMD_FILE_RELATIVE_PATH);
            }
        }

        string getCompressedFilePath(string repoPath)
        {
            DIBMDHandler DIBMDHandler = new DIBMDHandler($"{repoPath}/{Paths.DIBMD_FILE_RELATIVE_PATH}");
            string repoName = DIBMDHandler.GetName();

            return $"{repoPath}/{repoName + COMPRESSED_FILE_EXTENSION}";
        }

        public void InitRepo(string repoPath, string name, string author, string remote, int port = DEFAULT_PORT)
        {
            Directory.CreateDirectory($"{repoPath}/{Paths.DIB_DIRECTORY_RELATIVE_PATH}");
            File.Create($"{repoPath}/{Paths.DIBF_FILE_RELATIVE_PATH}").Close();
            createDIBIGNOREFile(repoPath);
            createDIBMDFile(repoPath, name, author, remote, port);
        }

        void createDIBMDFile(string repoPath, string name, string author, string remote, int port = DEFAULT_PORT)
        {
            DIBMDHandler DIBMDHandler = new DIBMDHandler($"{repoPath}/{Paths.DIBMD_FILE_RELATIVE_PATH}");
            DIBMDHandler.SetAuthor(author);
            DIBMDHandler.SetName(name);
            DIBMDHandler.SetRemoteAddress(remote);
            DIBMDHandler.SetPort(port);
        }

        void createDIBIGNOREFile(string repoPath)
        {
            string[] entries = new string[] { EXCLUDED_DIBMDHANDLER_PATH, EXCLUDED_DIB_DIRECTORY_PATH, EXCLUDED_DIB_EXE_PATH };
            File.WriteAllLines($"{repoPath}/{Paths.DIBIGNORE_FILE_RELATIVE_PATH}", entries);
        }
    }
}
