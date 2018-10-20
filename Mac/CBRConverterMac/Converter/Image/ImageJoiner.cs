using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using iTextSharp.text;
using AppKit;
using Foundation;
using CoreGraphics;


namespace CbrConverter
{
    internal class ImageJoiner
    {
        /// <summary>
        /// if needed, will merge the images base on page in the image names
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="imageNames"></param>
        /// <returns></returns>
        public Dictionary<PageImageIndex, NSImage> Merge(Dictionary<PageImageIndex, NSImage> imagesList, string outputPath)
        {
            var imageListByPage = imagesList.GroupBy(i => i.Key.PageIndex);
            var newImageList = new Dictionary<PageImageIndex, NSImage>();

            foreach (var imagesInfo in imageListByPage)
            {
                var imagesPage = new Dictionary<PageImageIndex, NSImage>();

                foreach (var item in imagesInfo)
                {
                    var image = item.Value;
                    if (item.Value == null)
                    {
                        var imagePath = string.Format(@"{0}\{1}", outputPath, item.Key.ImageName);
                        image = new NSImage(imagePath);
                    }

                    imagesPage.Add(item.Key, image);

                }

                //var imagesPage = imagesInfo.Select(i => i).ToDictionary(x => x.Key, x => x.Value);
                var keyValue = MergeGroup(imagesPage, outputPath);
                newImageList.Add(keyValue.Key, keyValue.Value);
            }

            return newImageList;
        }

        /// <summary>
        /// will merge a group of images into one
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="imageNames"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="index"></param>
        public KeyValuePair<PageImageIndex, AppKit.NSImage> MergeGroup(Dictionary<PageImageIndex, AppKit.NSImage> groupImages, string outputPath)
        {
            double maxWidth = 0, maxHeight = 0;
            int position = 0;

            foreach (var imageInfo in groupImages)
            {
                maxWidth = Math.Max(imageInfo.Value.Size.Width, maxWidth);
                maxHeight += imageInfo.Value.Size.Height;
            }

            // Create new image with max width and sum of all height
            var newImage = new NSImage(new CoreGraphics.CGSize(maxWidth, maxHeight));
            newImage.LockFocus();

            // merge all images
            foreach (var img in groupImages)
            {
                if (position == 0)
                {
                    img.Value.Draw(new CGPoint(), new CGRect(), NSCompositingOperation.DestinationAtop, 1);
                    position++;
                    maxHeight = img.Value.Size.Height;
                }
                else
                {
                    img.Value.Draw(new CGPoint(0, maxHeight), new CGRect(), NSCompositingOperation.DestinationAtop, 1);
                    maxHeight += img.Value.Size.Height;
                }

                img.Value.Dispose();
                var imagePath = string.Format(@"{0}\{1}", outputPath, img.Key.ImageName);
                File.Delete(imagePath);
            }

            var pageDetails = new PageImageIndex
            {
                PageIndex = groupImages.First().Key.PageIndex,
                ImageIndex = 1,
                ImageName = string.Format("{0:0000}_{1:0000}.jpg", groupImages.First().Key.PageIndex, 1)
            };

            var newImagePath = string.Format(@"{0}\{1}", outputPath, pageDetails.ImageName);
            var newImageData2 = newImage.AsTiff();
            newImageData2.Save(newImagePath, false);
            newImage.Dispose();

            return new KeyValuePair<PageImageIndex, NSImage>(pageDetails, null);
        }
    }
}
