using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EverLoader.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Checks if an object is in an array. 
        /// Example: number.In(3, 4, 7) returns true if number equals 3, 4 or 7.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="checklist"></param>
        /// <returns></returns>
        public static bool In<T>(this T item, params T[] checklist)
        {
            return checklist.Contains(item);
        }

        public static void Show<T>(this IProgress<T> progress, T msg)
        {
            progress.Report(msg); 
            Application.DoEvents();
        }

        public static FileInfo CopyToOverwriteIfNewer(this FileInfo sourceFile, string destFilePath)
        {
            var destFile = new FileInfo(destFilePath);
            if (!destFile.Exists || sourceFile.LastWriteTime > destFile.LastWriteTime)
            {
                return sourceFile.CopyTo(destFilePath, overwrite:true);
            }
            return null;
        }
    }
}
