using EverLoader.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EverLoader.Services
{
    public class ImageManager
    {
        private readonly UserSettingsManager _userSettingsManager;

        public ImageManager(UserSettingsManager userSettingsManager)
        {
            _userSettingsManager = userSettingsManager;
        }

        /// <summary>
        /// Resizes a source image and stores into target image paths
        /// </summary>
        /// <param name="filePath">Path to a jpg, png or gif</param>
        /// <returns></returns>
        public async Task ResizeImage(string sourceImagePath, GameInfo game, IEnumerable<ImageInfo> targets, bool saveOriginal = true)
        {
            byte[] fileBytes = null;
            if (sourceImagePath.StartsWith("http"))
            {
                using (var wc = new WebClient())
                {
                    fileBytes = await wc.DownloadDataTaskAsync(sourceImagePath);
                }
            }
            else
            {
                fileBytes = await File.ReadAllBytesAsync(sourceImagePath);
            }

            ResizeImage(fileBytes, game, targets, saveOriginal: saveOriginal);
        }

        public void ResizeImage(byte[] fileBytes, GameInfo game, IEnumerable<ImageInfo> targets, bool saveOriginal = true)
        {
            var q = _userSettingsManager.UserSettings.OptimizeImageSizes ? new nQuant.WuQuantizer() : null;
            using (var source = new ImageConverter().ConvertFrom(fileBytes) as Image)
            {
                foreach (var target in targets)
                {
                    if (saveOriginal)
                    {
                        //save source image as PNG in source folder
                        var origImagePath = GamesManager.GetSourceImagePath(target.LocalPath);
                        source.Save(origImagePath, ImageFormat.Png);
                    }

                    // if target is landscape (i.e. banner) => use smart cropping
                    // if difference between source and target image aspect ratio is less than 15% => use smart cropping
                    // else => resize with padding
                    double sourceAspectRatio = (double)source.Size.Width / source.Size.Height;
                    double targetAspectRatio = (double)target.Size.Width / target.Size.Height;
                    bool useSmartCropping = targetAspectRatio > 1 || Math.Abs((sourceAspectRatio - targetAspectRatio) / sourceAspectRatio) <= .15;

                    Size correctedSize;
                    Size offset;
                    if ((double)source.Width / source.Height > (double)target.Size.Width / target.Size.Height)
                    {
                        // source image is relatively wider than target
                        if (useSmartCropping)
                        {
                            //Cropping: remove parts of left and right side to fit original image in target size
                            correctedSize = new Size(source.Size.Height * target.Size.Width / target.Size.Height, source.Size.Height);
                            offset = new Size((source.Size.Width - correctedSize.Width) / 2, 0);
                        }
                        else
                        {
                            //Padding: try to fit original image in target size
                            correctedSize = new Size(target.Size.Width, target.Size.Width * source.Height / source.Width);
                            offset = new Size(0, (target.Size.Height - correctedSize.Height) / 2);
                        }
                    }
                    else
                    {
                        // source image is relatively taller than target
                        if (useSmartCropping)
                        {
                            correctedSize = new Size(source.Size.Width, source.Size.Width * target.Size.Height / target.Size.Width);
                            offset = new Size(0, (source.Size.Height - correctedSize.Height) / 2); //default for Middle

                            if (target.VerticalOffset != 0)
                            {
                                offset.Height = (source.Size.Height - correctedSize.Height) * (ImageInfo.MaxVerticalOffset + target.VerticalOffset) / (2 * ImageInfo.MaxVerticalOffset);
                            }
                        }
                        else
                        {
                            correctedSize = new Size(target.Size.Height * source.Width / source.Height, target.Size.Height);
                            offset = new Size((target.Size.Width - correctedSize.Width) / 2, 0);
                        }
                    }

                    using (var targetImg = new Bitmap(target.Size.Width, target.Size.Height))
                    {
                        using (var g = Graphics.FromImage(targetImg))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            if (useSmartCropping)
                            {
                                g.DrawImage(
                                image: source,
                                destRect: new Rectangle(0, 0, target.Size.Width, target.Size.Height),
                                srcRect: new Rectangle(offset.Width, offset.Height, correctedSize.Width, correctedSize.Height),
                                srcUnit: GraphicsUnit.Pixel);
                            }
                            else
                            {
                                g.DrawImage(
                                    image: source,
                                    destRect: new Rectangle(offset.Width, offset.Height, correctedSize.Width, correctedSize.Height),
                                    srcRect: new Rectangle(0, 0, source.Width, source.Height),
                                    srcUnit: GraphicsUnit.Pixel);
                            }
                        }

                        if (_userSettingsManager.UserSettings.OptimizeImageSizes)
                        {
                            using (var quantized = q.QuantizeImage(targetImg))
                            {
                                quantized.Save(target.LocalPath, ImageFormat.Png);
                            }
                        }
                        else
                        {
                            targetImg.Save(target.LocalPath, ImageFormat.Png);
                        }
                    }
                }
            }

            //now refresh gameInfo
            foreach (var gameImage in targets)
            {
                switch (gameImage.ImageType)
                {
                    case ImageType.Small: game.Image = gameImage.LocalPath; break;
                    case ImageType.Medium: game.ImageHD = gameImage.LocalPath; break;
                    case ImageType.Large: game.Image1080 = gameImage.LocalPath; break;
                    case ImageType.Banner:
                        game.ImageBanner = gameImage.LocalPath;
                        game.ImageBannerVerticalOffset = gameImage.VerticalOffset;
                        break;
                }
            }
        }
    }
}
