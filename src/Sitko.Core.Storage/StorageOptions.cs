using System;
using System.Collections.Generic;
using SixLabors.ImageSharp.Processing;

namespace Sitko.Core.Storage
{
    public abstract class StorageOptions : IStorageOptions
    {
        public Uri PublicUri { get; }

        public List<StorageImageSize> Thumbnails = new List<StorageImageSize>
        {
            new StorageImageSize(100, 100), new StorageImageSize(300, 300), new StorageImageSize(800, 800),
        };

        public StorageOptions(Uri publicUri)
        {
            PublicUri = publicUri;
        }
    }

    public class StorageImageSize
    {
        public StorageImageSize(int width, int height, ResizeMode mode = ResizeMode.Max, string? key = null)
        {
            Width = width;
            Height = height;
            Mode = mode;
            Key = key;
        }

        public int Width { get; }
        public int Height { get; }
        public ResizeMode Mode { get; }
        public string? Key { get; }
    }
}
