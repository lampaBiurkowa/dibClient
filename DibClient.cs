using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DibClient
{
    public class DibClient
    {
        const string DIB_DIRECTORY_RELATIVE_PATH = ".dib";
        const string DIBMD_FILE_RELATIVE_PATH = DIB_DIRECTORY_RELATIVE_PATH + "/.dibmd";
        const string DIBRM_FILE_RELATIVE_PATH = DIB_DIRECTORY_RELATIVE_PATH + "/.dibrm";
        const string DIBVER_FILE_PATH = ".dibver";

        const string APPS_DIRECTORY_PATH = "dibApps";
        const string UPDATER_DIRECTORY_PATH = "dibUpdater";

        Dictionary<string, int> versionsData = new Dictionary<string, int>();
        readonly WebClient webClient = new WebClient();

        public delegate void OnPercentageChanged(string appName, int percentage);
        public static event OnPercentageChanged PercentageChanged;

        string appName;

        public DibClient(string appName)
        {
            this.appName = appName;

            if (!Directory.Exists(APPS_DIRECTORY_PATH))
                Directory.CreateDirectory(APPS_DIRECTORY_PATH);

            if (!Directory.Exists(UPDATER_DIRECTORY_PATH))
                Directory.CreateDirectory(UPDATER_DIRECTORY_PATH);

            versionsData = DIBVERHandler.GetVersionsData(DIBVER_FILE_PATH);
        }

        public async Task UpdateToMaster(bool force = false)
        {
            string uri = $"https://localhost:5001/api/dibDownload/{appName}";
            if (!force && versionsData.ContainsKey(appName))
                uri += $"/master/{versionsData[appName]}";
            
            await update(uri, force);
        }

        async Task update(string uri, bool force)
        {
            //try
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
            }
           // catch
            //{
                System.Console.WriteLine($"zal");
            //}
        }

        void onDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
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
            string uri = $"https://localhost:5001/api/dibDownload/{appName}/{targetVersion}";
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
            return $"{APPS_DIRECTORY_PATH}/{appName}";
        }

        void clearAppDirectory()
        {
            Directory.Delete(getPathToApp(), true);
            Directory.CreateDirectory(getPathToApp());
        }

        void removeUnnecessaryFiles()
        {
            string pathToDIBRM = $"{getPathToUpdatePack()}/{DIBRM_FILE_RELATIVE_PATH}";
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

            if (versionsData.ContainsKey(appName))
                versionsData[appName] = currentVersion;
            else
                versionsData.Add(appName, currentVersion);

            DIBVERHandler.SaveVersionsData(DIBVER_FILE_PATH, versionsData);
        }

        int getVersion()
        {
            string pathToDIBMD = $"{getPathToApp()}/{DIBMD_FILE_RELATIVE_PATH}";
            IntPtr DIBMDHandler = CreateDIBMDHandlerClass(pathToDIBMD);
            return GetRepoVersion(DIBMDHandler);
        }

        [DllImport("DIBMDHandler.dll")]
        static extern IntPtr CreateDIBMDHandlerClass(string path);

        [DllImport("DIBMDHandler.dll")]
        static extern int GetRepoVersion(IntPtr DIBMDHandlerObject);

        void removeInstallationFiles()
        {
            Directory.Delete(UPDATER_DIRECTORY_PATH, true);
            Directory.Delete($"{getPathToApp()}/{DIB_DIRECTORY_RELATIVE_PATH}", true);
        }

        public void Uninstall()
        {
            removeAppFromDIBVERFile();
            if (Directory.Exists(getPathToApp()))
                Directory.Delete(getPathToApp(), true);

            Directory.Delete(UPDATER_DIRECTORY_PATH, true);
        }

        void removeAppFromDIBVERFile()
        {
            Dictionary<string, int> versionsData = DIBVERHandler.GetVersionsData(DIBVER_FILE_PATH);
            if (versionsData.ContainsKey(appName))
                versionsData.Remove(appName);

            DIBVERHandler.SaveVersionsData(DIBVER_FILE_PATH, versionsData);
        }
    }
}
