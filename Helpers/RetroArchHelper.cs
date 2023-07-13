using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using EverLoader.Extensions;

namespace EverLoader.Helpers;

public static class RetroArchHelper
{
    public static async Task DownloadAndExtract(string extractPath, IProgress<float> progress)
    {
        Uri uri = new Uri("https://eversd.com/onewebmedia/RetroArch_EverSD_latest.zip");
        string tempPath = Path.GetTempFileName();

        try
        {
            // download zip archive from EverSD homepage
            using HttpClient client = CreateHttpClient();
            await using var file = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            await client.DownloadAsync(uri, file, progress);

            // update progress form to show that extraction is running (sadly has no progress)
            progress.Report(2);

            // extract zip archive to SD card
            using var zip = new ZipArchive(file);
            await Task.Run(() => zip.ExtractToDirectory(extractPath));

            // close file stream
            file.Close();
        }
        finally
        {
            // delete temp file
            File.Delete(tempPath);
        }        
    }

    private static HttpClient CreateHttpClient()
    {
        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ClientCertificateOptions = ClientCertificateOption.Automatic;
        
        return new HttpClient(clientHandler);
    }
}