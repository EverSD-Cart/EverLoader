using System.Linq;
using System.IO;

namespace EverLoader.Helpers
{
    //https://stackoverflow.com/questions/1232398/how-to-programatically-format-an-sd-card-with-fat16
    public static class SDCardHelper
    {
        public static DriveInfo[] FindRemovableDrives()
        {
            return DriveInfo.GetDrives()
                //Take only removable drives into consideration as a SD card candidates
                .Where(drive => drive.DriveType == DriveType.Removable)
                .Where(drive => drive.IsReady)
                .ToArray();
        }
    }
}
