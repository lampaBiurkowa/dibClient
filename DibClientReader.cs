using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DibClient
{
    public class DibClientReader
    {
        const string APPS_DIRECTORY_PATH = "dibApps";
        const string UPDATER_DIRECTORY_PATH = "dibUpdater";

        Dictionary<string, int> versionsData = new Dictionary<string, int>();
        readonly WebClient webClient = new WebClient();

        public delegate void OnFileSizeInfoGet(long bytes);
        public static event OnFileSizeInfoGet FileSizeInfoGet;
        public delegate void OnPercentageChanged(string appName, int percentage);
        public static event OnPercentageChanged PercentageChanged;
        public delegate void OnInstalled(string appName);
        public static event OnInstalled Installed;

        string appName;
        string appDirectoryPath;
        bool fileSizeInfoGet = false;
        static HttpClient httpClient = new HttpClient();

        public DibClientReader(string appName, string appDirectoryPath = APPS_DIRECTORY_PATH)
        {
            this.appName = appName;
            this.appDirectoryPath = appDirectoryPath;
        }

        void createResourcesIfDoesntExist()
        {
            if (!Directory.Exists(appDirectoryPath))
                Directory.CreateDirectory(appDirectoryPath);

            if (!Directory.Exists(UPDATER_DIRECTORY_PATH))
                Directory.CreateDirectory(UPDATER_DIRECTORY_PATH);
        }

        public async Task UpdateToMaster(bool force = false)
        {
            createResourcesIfDoesntExist();

            string uri = $"http://caps.fail:5000/api/dibDownload/{appName}";
            if (!force && versionsData.ContainsKey(appName))
                uri += $"/master/{versionsData[appName]}";
            
            await update(uri, force);
        }

        async Task update(string uri, bool force)
        {
            ////try
            {
                webClient.DownloadProgressChanged += onDownloadProgressChanged;

                string downloadedZipPath = getDownloadedZipDestinationPath();
                await webClient.DownloadFileTaskAsync(new Uri(uri), downloadedZipPath);

                string destinationPath = getPathToUpdatePack();
                if (Directory.Exists(destinationPath))
                    Directory.Delete(destinationPath, true);

                ZipFile.ExtractToDirectory(downloadedZipPath, destinationPath);
                install(force);
                updateAppVersion();
                removeInstallationFiles();
                Installed?.Invoke(appName);
                fileSizeInfoGet = false;
            }
            //catch
            {
                System.Console.WriteLine($"zal");
            }
        }

        void onDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (!fileSizeInfoGet)
            {
                FileSizeInfoGet?.Invoke(e.TotalBytesToReceive);
                fileSizeInfoGet = true;
            }

            PercentageChanged?.Invoke(appName, e.ProgressPercentage);
        }

        string getDownloadedZipDestinationPath()
        {
            return $"{UPDATER_DIRECTORY_PATH}/{appName}.zip";
        }

        string getPathToUpdatePack()
        {
            return $"{UPDATER_DIRECTORY_PATH}/{appName}";
        }

        public async Task UpdateToVersion(int targetVersion, bool force = false)
        {
            createResourcesIfDoesntExist();

            string uri = $"http://caps.fail:5000/api/dibDownload/{appName}/{targetVersion}";
            versionsData = DIBVERHandler.GetVersionsData(Paths.DIBVER_FILE_PATH);
            if (!force && versionsData.ContainsKey(appName))
                uri += $"/{versionsData[appName]}";

            await update(uri, force);
        }

        void install(bool force)
        {
            if (!Directory.Exists(getPathToApp()))
                Directory.CreateDirectory(getPathToApp());

            if (force)
                clearAppDirectory();
            else
                removeUnnecessaryFiles();

            copyNecesseryFiles();
        }

        string getPathToApp()
        {
            return $"{appDirectoryPath}/{appName}";
        }

        void clearAppDirectory()
        {
            Directory.Delete(getPathToApp(), true);
            Directory.CreateDirectory(getPathToApp());
        }

        void removeUnnecessaryFiles()
        {
            string pathToDIBRM = $"{getPathToUpdatePack()}/{Paths.DIBRM_FILE_RELATIVE_PATH}";
            if (!File.Exists(pathToDIBRM))
                return;

            string[] filesToRemovePaths = File.ReadAllLines(pathToDIBRM);
            foreach (string path in filesToRemovePaths)
                File.Delete($"{getPathToApp()}/{path}");
        }

        void copyNecesseryFiles()
        {
            string pathToUpdatePack = getPathToUpdatePack();
            foreach (string path in Directory.GetFiles(pathToUpdatePack, "*", SearchOption.AllDirectories))
            {
                string relativePart = path.Replace(pathToUpdatePack, "");
                string pathInApp = $"{getPathToApp()}/{relativePart}";
                createDirectoryFromPath(Path.GetFullPath(Path.GetDirectoryName(pathInApp)));
                File.Copy(path, pathInApp, true);
            }
        }

        void createDirectoryFromPath(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                createDirectoryFromPath(Path.GetDirectoryName(path));

            Directory.CreateDirectory(path);
        }

        void updateAppVersion()
        {
            int currentVersion = getVersion();

            versionsData = DIBVERHandler.GetVersionsData(Paths.DIBVER_FILE_PATH);
            if (versionsData.ContainsKey(appName))
                versionsData[appName] = currentVersion;
            else
                versionsData.Add(appName, currentVersion);

            DIBVERHandler.SaveVersionsData(Paths.DIBVER_FILE_PATH, versionsData);
        }

        int getVersion()
        {
            string pathToDIBMD = $"{getPathToApp()}/{Paths.DIBMD_FILE_RELATIVE_PATH}";
            DIBMDHandler handler = new DIBMDHandler(pathToDIBMD);
            return handler.GetVersion();
        }

        void removeInstallationFiles()
        {
            Directory.Delete(UPDATER_DIRECTORY_PATH, true);
            Directory.Delete($"{getPathToApp()}/{Paths.DIB_DIRECTORY_RELATIVE_PATH}", true);
        }

        public async Task Uninstall()
        {
            removeAppFromDIBVERFile();
            if (Directory.Exists(getPathToApp()))
                Directory.Delete(getPathToApp(), true);
        }

        void removeAppFromDIBVERFile()
        {
            Dictionary<string, int> versionsData = DIBVERHandler.GetVersionsData(Paths.DIBVER_FILE_PATH);
            if (versionsData.ContainsKey(appName))
                versionsData.Remove(appName);

            DIBVERHandler.SaveVersionsData(Paths.DIBVER_FILE_PATH, versionsData);
        }

        public async Task<bool> IsUpdateAvailable()
        {
            string uri = $"http://caps.fail:5000/api/dibInfo/GetVersionInfo/{appName}";
            string versionStr = await httpClient.GetStringAsync(uri);
            int versionNumber = int.Parse(versionStr);

            Dictionary<string, int> versionsData = DIBVERHandler.GetVersionsData(Paths.DIBVER_FILE_PATH);
            if (versionsData.ContainsKey(appName))
                return !(versionNumber == versionsData[appName]);

            return false;
        }

        public async Task<bool> IsInstalled()
        {
            Dictionary<string, int> versionsData = DIBVERHandler.GetVersionsData(Paths.DIBVER_FILE_PATH);
            return versionsData.ContainsKey(appName);
        }
    }
}
