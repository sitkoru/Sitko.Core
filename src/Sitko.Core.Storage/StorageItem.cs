using System;
using System.Collections.Generic;
using System.Linq;

namespace Sitko.Core.Storage
{
    public class StorageItem
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public Uri PublicUri { get; set; }
        public string FilePath { get; set; }

        public string Path { get; set; }
        public StorageItemType Type { get; set; } = StorageItemType.Other;
        public StorageItemImageInfo? ImageInfo { get; set; }

        public string StorageFileName => FilePath.Substring(FilePath.LastIndexOf('/') + 1);
        private readonly string[] _units = {"bytes", "KB", "MB", "GB", "TB", "PB"};

        public string HumanSize
        {
            get
            {
                if (FileSize < 1)
                {
                    return "-";
                }

                var unit = 0;

                double size = FileSize;
                while (size >= 1024)
                {
                    size /= 1024;
                    unit++;
                }

                return Math.Round(size, 2) + ' ' + _units[unit];
            }
        }

        public StorageItemImageThumbnail? GetThumbnailByKey(string key)
        {
            return ImageInfo?.Thumbnails?.Where(t => t.Key == key).FirstOrDefault();
        }

        public StorageItemImageThumbnail? GetThumbnailByWidth(int width)
        {
            return ImageInfo?.Thumbnails.Where(t => t.Width >= width).OrderBy(t => t.Width).FirstOrDefault();
        }

        public StorageItemImageThumbnail? GetThumbnailByHeight(int height)
        {
            return ImageInfo?.Thumbnails.Where(t => t.Height >= height).OrderBy(t => t.Height)
                .FirstOrDefault();
        }

        public Uri GetImageUriByWidth(int width)
        {
            var thumbnail = GetThumbnailByWidth(width);
            return thumbnail != null ? thumbnail.PublicUri : PublicUri;
        }

        public Uri GetImageUriByHeight(int height)
        {
            var thumbnail = GetThumbnailByHeight(height);
            return thumbnail != null ? thumbnail.PublicUri : PublicUri;
        }
    }

    public enum StorageItemType
    {
        Image = 1,
        Other = 2
    }

    public class StorageItemImageInfo
    {
        public double VerticalResolution { get; set; }
        public double HorizontalResolution { get; set; }

        public List<StorageItemImageThumbnail> Thumbnails { get; set; } = new List<StorageItemImageThumbnail>();
    }

    public class StorageItemImageThumbnail
    {
        public Uri PublicUri { get; set; }
        public string FilePath { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public string Key { get; set; }

        public StorageItemImageThumbnail()
        {
        }

        public StorageItemImageThumbnail(Uri publicUri, string filePath, int width, int height, string key)
        {
            PublicUri = publicUri;
            FilePath = filePath;
            Width = width;
            Height = height;
            Key = key;
        }
    }
}
