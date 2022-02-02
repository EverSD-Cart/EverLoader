using EverLoader.Extensions;
using EverLoader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EverLoader.Services
{
    public class DownloadManager
    {
        public async Task<string> GetDownloadedFilePath(Uri zipUrl, string relativeFilePathInZip)
        {
            if (!Path.GetExtension(zipUrl.AbsolutePath).In(".zip", ".gz"))
            {
                throw new ArgumentException("Can only download .zip of .gz files");
            }

            var cachedZipDirectoryPath = $"{Constants.APP_ROOT_FOLDER}cache/{zipUrl.Host}{zipUrl.AbsolutePath}/";
            var cachedAbsoluteFilePath = $"{cachedZipDirectoryPath}{relativeFilePathInZip}";
            if (File.Exists(cachedAbsoluteFilePath))
            {
                return cachedAbsoluteFilePath;
            }

            //not in cache yet, so download
            Directory.CreateDirectory(cachedZipDirectoryPath);

            using (var client = new HttpClient())
            using (var response = await client.GetAsync(zipUrl))
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                if (Path.GetExtension(zipUrl.AbsolutePath) == ".gz")
                {
                    using (var outputFile = File.OpenWrite(cachedAbsoluteFilePath))
                    using (var archive = new GZipStream(responseStream, CompressionMode.Decompress))
                    {
                        archive.CopyTo(outputFile);
                    }
                }
                if (Path.GetExtension(zipUrl.AbsolutePath) == ".zip")
                {
                    using (var archive = new ZipArchive(responseStream))
                    {
                        archive.ExtractToDirectory(cachedZipDirectoryPath);
                    }
                }
            }
            if (!File.Exists(cachedAbsoluteFilePath))
            {
                throw new ArgumentException($"File {relativeFilePathInZip} does not exist in {zipUrl.AbsoluteUri}");
            }
            return cachedAbsoluteFilePath;
        }
    }
}
