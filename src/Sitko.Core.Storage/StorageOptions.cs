using SixLabors.ImageSharp.Processing;

namespace Sitko.Core.Storage
{
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
