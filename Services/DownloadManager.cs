using EverLoader.Extensions;
using EverLoader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace EverLoader.Services
{
    public class DownloadManager
    {
        private List<string> _sessionFiles = new List<string>();

        public async Task<string> GetDownloadedFilePath(Uri url, string relativeFilePathInZip)
        {
            if (relativeFilePathInZip != null && !Path.GetExtension(url.AbsolutePath).In(".zip", ".gz"))
            {
                throw new ArgumentException("Can only extract .zip or .gz files");
            }

            var localFilePath = $"{Constants.APP_ROOT_FOLDER}cache/{url.Host}{url.AbsolutePath}";
            string localUnzipFolder = null;
            if (relativeFilePathInZip != null)
            {
                localUnzipFolder = Path.Combine(Path.GetDirectoryName(localFilePath), Path.GetFileNameWithoutExtension(localFilePath));
                localFilePath = Path.Combine(localUnzipFolder, relativeFilePathInZip);
            }

            //if file exists in cache folder and newer than 1 day, then return file
            var cachedFileInfo = new FileInfo(localFilePath);
            if (cachedFileInfo.Exists && _sessionFiles.Contains(localFilePath))
            {
                return localFilePath;
            }

            //first create/ensure target directory structure
            Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

                    //if cached file already exists, first do head request to check if cached file is still up-to-date
                    var cacheFileUpToDate = false;
                    if (cachedFileInfo.Exists)
                    {
                        using (var headResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)))
                        {
                            var headerLastModified = headResponse.Content.Headers?.LastModified;
                            cacheFileUpToDate = headerLastModified.HasValue 
                                && headerLastModified.Value.UtcDateTime < cachedFileInfo.LastWriteTime.ToUniversalTime();
                        }
                    }

                    if (!cacheFileUpToDate)
                    {
                        using (var response = await client.GetAsync(url))
                        {
                            using (var responseStream = await response.Content.ReadAsStreamAsync())
                            {
                                if (relativeFilePathInZip == null)
                                {
                                    using (var outputFile = File.OpenWrite(localFilePath))
                                    {
                                        await responseStream.CopyToAsync(outputFile);
                                    }
                                }
                                else if (Path.GetExtension(url.AbsolutePath) == ".gz")
                                {
                                    using (var outputFile = File.OpenWrite(localFilePath))
                                    using (var archive = new GZipStream(responseStream, CompressionMode.Decompress))
                                    {
                                        await archive.CopyToAsync(outputFile);
                                    }
                                }
                                else if (Path.GetExtension(url.AbsolutePath) == ".zip")
                                {
                                    using (var archive = new ZipArchive(responseStream))
                                    {
                                        archive.ExtractToDirectory(localUnzipFolder, overwriteFiles:true);
                                    }
                                    UpdateLastWriteTime(new DirectoryInfo(localUnzipFolder), DateTime.Now);
                                }
                            }
                        }
                    }
                }
            }
            catch { /* rely on next File.Exists check to throw exception */ }
            if (!File.Exists(localFilePath))
            {
                throw new ArgumentException($"File {relativeFilePathInZip} does not exist in {url.AbsoluteUri}");
            }
            if (!_sessionFiles.Contains(localFilePath)) _sessionFiles.Add(localFilePath);
            return localFilePath;
        }

        private void UpdateLastWriteTime(DirectoryInfo dir, DateTime dateTime) 
        {
            if (!dir.Exists) return;
            foreach (var file in dir.EnumerateFiles()) file.LastWriteTime = dateTime;
            foreach (var subDir in dir.GetDirectories()) UpdateLastWriteTime(subDir, dateTime);
        }
    }
}
