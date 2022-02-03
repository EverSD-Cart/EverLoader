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
            if (File.Exists(localFilePath))
            {
                return localFilePath;
            }

            //not in cache yet, so download

            //first create target directory structure
            Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));

            using (var client = new HttpClient())
            using (var response = await client.GetAsync(url))
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
                        archive.ExtractToDirectory(localUnzipFolder);
                    }
                }
            }
            if (!File.Exists(localFilePath))
            {
                throw new ArgumentException($"File {relativeFilePathInZip} does not exist in {url.AbsoluteUri}");
            }
            return localFilePath;
        }
    }
}
