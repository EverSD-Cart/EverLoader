#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EverLoader.Extensions;
using EverLoader.Helpers.RetroArch;
using Flurl.Http;

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

        if (CachedVersionExists(extractPath))
        {
            string cachedVersion = GetCachedVersionPath(extractPath);
            
            // check if newer version is available on server
            if (await NewVersionExists(extractPath))
            {
                // delete existing cached file
                if (File.Exists(cachedVersion)) File.Delete(cachedVersion);
                
                await DownloadAndExtract(extractPath, progress, tempPath, uri);
            } 
            else 
            {
                // use cached version

                // update progress form to show that extraction is running (sadly has no progress)
                progress.Report(2);

                await Task.Run(() => ZipFile.ExtractToDirectory(cachedVersion, extractPath, true));
            }
        }
        else
        {
            try
            {
                await DownloadAndExtract(extractPath, progress, tempPath, uri);
            }
            finally
            {
                // delete temp file
                File.Delete(tempPath);
            }
        }
    }

    private static async Task DownloadAndExtract(string extractPath, IProgress<float> progress, string tempPath, Uri uri)
    {
        // download zip archive from EverSD homepage
        using HttpClient client = CreateHttpClient();
        await using var file = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        await client.DownloadAsync(uri, file, progress);
        file.Close();

        // update progress form to show that extraction is running (sadly has no progress)
        progress.Report(2);

        // extract zip archive to SD card, overwrite existing files
        await Task.Run(() => ZipFile.ExtractToDirectory(tempPath, extractPath, true));

        // save zip file to cache folder
        CopyToCache(extractPath, tempPath);
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
        var relevantSubfolders = new[] { "assets" };
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

    /// <summary>
    /// Get RetroArch version that's currently available on the EverSD website
    /// </summary>
    /// <returns>RetroArch version</returns>
    private static async Task<decimal?> GetServerVersion()
    {
        const string url = "https://eversd.com/onewebmedia/RetroArch_EverSD_version.json";

        try
        {
            var versionResult = await url
                .WithHeader("User-Agent", "EverLoader")
                .GetJsonAsync();

            return decimal.Parse(versionResult.version, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Get cached version path
    /// </summary>
    /// <param name="sdRootDrive"></param>
    /// <returns></returns>
    private static string GetCachedVersionPath(string sdRootDrive) =>
        Path.Combine(sdRootDrive, "cache", "RetroArch_EverSD.zip");

    private static bool CachedVersionExists(string sdDrive)
    {
        try
        {
            string path = GetCachedVersionPath(sdDrive);
            return File.Exists(path);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void CreateCacheFolder(string sdDrive)
    {
        string? pathRoot = Path.GetPathRoot(sdDrive);
        if (pathRoot is null) throw new ArgumentException("Path to SD drive can't be null", nameof(sdDrive));
        
        string cacheDirectory = Path.Combine(pathRoot, "cache");

        if (!Directory.Exists(cacheDirectory)) Directory.CreateDirectory(cacheDirectory);
    }

    private static decimal? GetCachedVersion(string sdRootDrive)
    {
        string filePath = GetCachedVersionPath(sdRootDrive);
        if (!File.Exists(filePath)) return null;

        using var zip = ZipFile.OpenRead(filePath);
        var entry = zip.GetEntry("RetroArch_EverSD_version.json");
        if (entry is null) return null;

        using var fileStream = entry.Open();
        var result = JsonSerializer.Deserialize<VersionResult>(fileStream);

        if (result is null) return null;

        try
        {
            return decimal.Parse(result.Version, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<bool> NewVersionExists(string sdRootDrive)
    {
        decimal? cachedVersion = GetCachedVersion(sdRootDrive);
        decimal? serverVersion = await GetServerVersion();

        if (cachedVersion is null || serverVersion is null) return false;
        
        return serverVersion > cachedVersion;
    }

    private static void CopyToCache(string sdRootDrive, string tempFile)
    {
        if (!File.Exists(tempFile)) return;
        
        CreateCacheFolder(sdRootDrive);
        File.Copy(tempFile, GetCachedVersionPath(sdRootDrive));
    }
}