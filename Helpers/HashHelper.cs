using Force.Crc32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EverLoader.Helpers
{
    public static class HashHelper
    {
        public static (string Crc32, string Md5) CalculateHashcodes(string filePath)
        {
            using var md5 = MD5.Create(); //TODO: make this helper non-static and create MD5 object during construction
            var gameBytes = File.ReadAllBytes(filePath);
            var fileCrc32 = Crc32Algorithm.Compute(gameBytes).ToString("X8");
            var fileMd5 = BitConverter.ToString(md5.ComputeHash(gameBytes)).ToLower().Replace("-", "");
            return (fileCrc32, fileMd5);
        }
    }
}
