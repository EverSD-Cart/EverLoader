#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EverLoader.Extensions;

namespace EverLoader.Helpers;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public static class RetroArchHelper
{
    /// <summary>
    /// Check if the RetroArch directory is not existing or empty
    /// </summary>
    /// <param name="sdDrive">SD card drive</param>
    /// <returns>true if the directory does not exist or is empty</returns>
    public static bool IsAlreadyOnSdCard(string sdDrive)
    {
        string? path = GetRetroArchPath(sdDrive);
        if (path is null) throw new Exception("Failed to retrieve RetroArch path on SD card");

        return Directory.Exists(path) && DirectoryContainsRelevantSubfolder(path);
    }

    /// <summary>
    /// Download and extract the latest RetroArch EverSD files 
    /// </summary>
    /// <param name="extractPath">Extract path</param>
    /// <param name="progress">Progress</param>
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

            // extract zip archive to SD card, overwrite existing files
            using var zip = new ZipArchive(file);
            await Task.Run(() => zip.ExtractToDirectory(extractPath, true));

            // close file stream
            file.Close();
        }
        finally
        {
            // delete temp file
            File.Delete(tempPath);
        }        
    }

    /// <summary>
    /// Create HTTP client
    /// </summary>
    /// <returns>HttpClient</returns>
    private static HttpClient CreateHttpClient()
    {
        HttpClientHandler clientHandler = new HttpClientHandler();
        clientHandler.ClientCertificateOptions = ClientCertificateOption.Automatic;
        
        return new HttpClient(clientHandler);
    }
    
    /// <summary>
    /// Directory contains relevant subfolders
    /// </summary>
    /// <param name="path">RetroArch path on SD card</param>
    /// <returns>True if subfolders exist</returns>
    private static bool DirectoryContainsRelevantSubfolder(string path)
    {
        var relevantSubfolders = new[] { "assets", "config" };
        var directories = Directory.EnumerateDirectories(path);

        return directories.Any(directory => relevantSubfolders.Contains(RemoveRetroArchPath(directory, path)));
    }

    /// <summary>
    /// Remove RetroArch path from complete path
    /// </summary>
    /// <param name="directory">Directory to process</param>
    /// <param name="retroArchPath">RetroArch path on SD card</param>
    /// <returns>Path excluding the RetroArch root folder</returns>
    private static string RemoveRetroArchPath(string directory, string retroArchPath) =>
        directory[(retroArchPath.Length + 1)..];

    /// <summary>
    /// Get RetroArch path on SD card
    /// </summary>
    /// <param name="sdDrive">SD card drive</param>
    private static string? GetRetroArchPath(string sdDrive)
    {
        string? pathRoot = Path.GetPathRoot(sdDrive);
        return pathRoot is null
            ? null
            : Path.Combine(pathRoot, "retroarch");
    }
}