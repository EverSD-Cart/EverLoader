using System.Drawing;

namespace EverLoader.Models
{
    public class ImageInfo
    {
        public const int MaxVerticalOffset = 5;

        public string LocalPath { get; set; }
        public ImageType ImageType { get; set; }
        public Size Size { get; set; }

        public int VerticalOffset { get; set; }
    }
}
