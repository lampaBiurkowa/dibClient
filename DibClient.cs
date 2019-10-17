using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DibClient
{
    public static class DibClient
    {
        const string DIB_DIRECTORY_RELATIVE_PATH = ".dib";
        const string DIBMD_FILE_RELATIVE_PATH = DIB_DIRECTORY_RELATIVE_PATH + "/.dibmd";
        const string DIBRM_FILE_RELATIVE_PATH = DIB_DIRECTORY_RELATIVE_PATH + "/.dibrm";
        const string DIBVER_FILE_PATH = ".dibver";

        const string APPS_DIRECTORY_PATH = "dibApps";
        const string ZIPS_DIRECTORY_PATH = "dibUpdater";

        static Dictionary<string, int> versionsData = new Dictionary<string, int>();
        static readonly HttpClient httpClient = new HttpClient();

        static DibClient()
        {
            if (!Directory.Exists(APPS_DIRECTORY_PATH))
                Directory.CreateDirectory(APPS_DIRECTORY_PATH);

            if (!Directory.Exists(ZIPS_DIRECTORY_PATH))
                Directory.CreateDirectory(ZIPS_DIRECTORY_PATH);

            versionsData = DIBVERHandler.GetVersionsData(DIBVER_FILE_PATH);
        }

        public async static Task UpdateToMaster(string appName, bool force = false)
        {
            string uri = $"https://localhost:5001/api/dibDownload/{appName}";
            if (!force && versionsData.ContainsKey(appName))
                uri += $"/master/{versionsData[appName]}";
            
            await update(uri, appName, force);
        }

        static async Task update(string uri, string appName, bool force)
        {
            try
            {
                byte[] responseBody = await httpClient.GetByteArrayAsync(uri);
                MemoryStream stream = new MemoryStream(responseBody);
                ZipArchive zipArchive = new ZipArchive(stream);

                string destinationPath = getPathToUpdatePack(appName);
                if (Directory.Exists(destinationPath))
                    Directory.Delete(destinationPath, true);
                zipArchive.ExtractToDirectory(destinationPath);

                stream.Dispose();

                install(appName, force);
                updateAppVersion(appName);
                removeInstallationFiles(appName);
            }
            catch (HttpRequestException e)
            {
                System.Console.WriteLine($"zal :{e.Message}");
            }
        }

        static string getPathToUpdatePack(string appName)
        {
            return $"{ZIPS_DIRECTORY_PATH}/{appName}";
        }

        public static async Task UpdateToVersion(string appName, int targetVersion, bool force = false)
        {
            string uri = $"https://localhost:5001/api/dibDownload/{appName}/{targetVersion}";
            if (!force && versionsData.ContainsKey(appName))
                uri += $"/{versionsData[appName]}";

            await update(uri, appName, force);
        }

        static void install(string appName, bool force)
        {
            if (!Directory.Exists(getPathToApp(appName)))
                Directory.CreateDirectory(getPathToApp(appName));

            if (force)
                clearAppDirectory(appName);
            else
                removeUnnecessaryFiles(appName);

            copyNecesseryFiles(appName);
        }

        static string getPathToApp(string appName)
        {
            return $"{APPS_DIRECTORY_PATH}/{appName}";
        }

        static void clearAppDirectory(string appName)
        {
            Directory.Delete(getPathToApp(appName), true);
            Directory.CreateDirectory(getPathToApp(appName));
        }

        static void removeUnnecessaryFiles(string appName)
        {
            string pathToDIBRM = $"{getPathToUpdatePack(appName)}/{DIBRM_FILE_RELATIVE_PATH}";
            if (!File.Exists(pathToDIBRM))
                return;

            string[] filesToRemovePaths = File.ReadAllLines(pathToDIBRM);
            foreach (string path in filesToRemovePaths)
                File.Delete($"{getPathToApp(appName)}/{path}");
        }

        static void copyNecesseryFiles(string appName)
        {
            string pathToUpdatePack = getPathToUpdatePack(appName);
            foreach (string path in Directory.GetFiles(pathToUpdatePack, "*", SearchOption.AllDirectories))
            {
                string relativePart = path.Replace(pathToUpdatePack, "");
                string pathInApp = $"{getPathToApp(appName)}/{relativePart}";
                createDirectoryFromPath(Path.GetFullPath(Path.GetDirectoryName(pathInApp)));
                File.Copy(path, pathInApp, true);
            }
        }

        static void createDirectoryFromPath(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                createDirectoryFromPath(Path.GetDirectoryName(path));
            
            Directory.CreateDirectory(path);
        }

        static void updateAppVersion(string appName)
        {
            int currentVersion = getVersion(appName);

            if (versionsData.ContainsKey(appName))
                versionsData[appName] = currentVersion;
            else
                versionsData.Add(appName, currentVersion);

            DIBVERHandler.SaveVersionsData(DIBVER_FILE_PATH, versionsData);
        }

        static int getVersion(string appName)
        {
            string pathToDIBMD = $"{getPathToApp(appName)}/{DIBMD_FILE_RELATIVE_PATH}";
            IntPtr DIBMDHandler = CreateDIBMDHandlerClass(pathToDIBMD);
            return GetRepoVersion(DIBMDHandler);
        }

        [DllImport("DIBMDHandler.dll")]
        static public extern IntPtr CreateDIBMDHandlerClass(string path);

        [DllImport("DIBMDHandler.dll")]
        static public extern int GetRepoVersion(IntPtr DIBMDHandlerObject);

        static void removeInstallationFiles(string appName)
        {
            Directory.Delete(ZIPS_DIRECTORY_PATH, true);
            Directory.Delete($"{getPathToApp(appName)}/{DIB_DIRECTORY_RELATIVE_PATH}", true);
        }
    }
}
