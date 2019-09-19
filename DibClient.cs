using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace DibClient
{
    public static class DibClient
    {
        const string ZIPS_DIRECTORY_PATH = "dibUpdater";

        static Dictionary<string, int> versionsData = new Dictionary<string, int>();
        static readonly HttpClient httpClient = new HttpClient();

        static DibClient()
        {
            if (!Directory.Exists(ZIPS_DIRECTORY_PATH))
                Directory.CreateDirectory(ZIPS_DIRECTORY_PATH);
        }

        public static async void UpdateToMaster(string appName, bool force = false)
        {
            try
            {
                string uri = $"https://localhost:5001/api/dibDownload/{appName}";
                HttpResponseMessage response = await httpClient.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                byte[] responseBody = await response.Content.ReadAsByteArrayAsync();

                MemoryStream stream = new MemoryStream(responseBody);
                ZipArchive zipArchive = new ZipArchive(stream);

                string destinationPath = $"{ZIPS_DIRECTORY_PATH}/{appName}";
                Directory.Delete(destinationPath, true);
                zipArchive.ExtractToDirectory(destinationPath);

                stream.Dispose();
            }
            catch (HttpRequestException e)
            {
                System.Console.WriteLine($"zal :{e.Message}");
            }
        }
    }
}
