using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Sitko.Core.Storage
{
    public abstract class Storage<T> : IStorage<T> where T : IStorageOptions
    {
        private readonly ILogger<Storage<T>> _logger;
        private readonly StorageOptions _options;

        protected Storage(StorageOptions options, ILogger<Storage<T>> logger)
        {
            _logger = logger;
            _options = options;
        }

        public async Task<StorageItem> SaveFileAsync(Stream file, string fileName, string path)
        {
            string destinationPath = GetDestinationPath(fileName, path);

            var storageItem = CreateStorageItem(file, fileName, destinationPath);

            return await SaveStorageItemAsync(file, path, destinationPath, storageItem);
        }

        private async Task<StorageItem> SaveStorageItemAsync(Stream file, string path, string destinationPath,
            StorageItem storageItem)
        {
            file.Seek(0, SeekOrigin.Begin);
            await DoSaveAsync(destinationPath, file);
            _logger.LogInformation("File saved to {path}", path);
            return storageItem;
        }

        private string GetDestinationPath(string fileName, string path)
        {
            var destinationName = GetStorageFileName(fileName);
            var destinationPath = $"{path}/{destinationName}";
            return destinationPath;
        }

        private StorageItem CreateStorageItem(Stream file, string fileName, string destinationPath)
        {
            var storageItem = new StorageItem
            {
                FileName = fileName,
                FileSize = file.Length,
                FilePath = destinationPath,
                Path = Path.GetDirectoryName(destinationPath)?.Replace("\\", "/"),
                PublicUri = new Uri($"{_options.PublicUri}/{destinationPath}")
            };
            return storageItem;
        }

        public async Task<StorageItem> SaveImageAsync(Stream file, string fileName, string path,
            List<StorageImageSize>? sizes = null)
        {
            string destinationPath = GetDestinationPath(fileName, path);

            var storageItem = CreateStorageItem(file, fileName, destinationPath);

            await ProcessImageAsync(storageItem, file, destinationPath, sizes);

            return await SaveStorageItemAsync(file, path, destinationPath, storageItem);
        }

        protected abstract Task<bool> DoSaveAsync(string path, Stream file);


        public abstract Task<bool> DeleteFileAsync(string filePath);

        protected string GetStorageFileName(string fileName)
        {
            var extension = fileName.Substring(fileName.LastIndexOf('.'));
            return Guid.NewGuid() + extension;
        }

        private async Task ProcessImageAsync(StorageItem storageItem, Stream file,
            string destinationPath, List<StorageImageSize>? sizes = null)
        {
            file.Seek(0, SeekOrigin.Begin);
            using var image = Image.Load<Rgba32>(file);
            storageItem.Type = StorageItemType.Image;
            storageItem.ImageInfo = new StorageItemImageInfo
            {
                VerticalResolution = image.Height, HorizontalResolution = image.Width
            };

            sizes ??= _options.Thumbnails;

            if (sizes != null && sizes.Any())
            {
                storageItem.ImageInfo.Thumbnails = new List<StorageItemImageThumbnail>();
                foreach (var size in sizes)
                {
                    var thumb = await CreateThumbnailAsync(image, size, destinationPath, storageItem.StorageFileName);
                    storageItem.ImageInfo.Thumbnails.Add(thumb);
                }
            }
        }

        private async Task<StorageItemImageThumbnail> CreateThumbnailAsync(Image<Rgba32> image, StorageImageSize size,
            string destinationPath, string fileName)
        {
            var thumb = image.Clone();
            thumb.Mutate(i =>
                i.Resize(new ResizeOptions {Size = new Size(size.Width, size.Height), Mode = size.Mode}));
            var thumbFileName = $"{thumb.Width.ToString()}_{thumb.Height.ToString()}_{fileName}";
            var thumbStream = new MemoryStream();
            var ext = fileName.Substring(fileName.LastIndexOf('.')).ToLowerInvariant();
            IImageFormat format = ext switch
            {
                ".png" => PngFormat.Instance,
                ".jpg" => JpegFormat.Instance,
                ".jpeg" => JpegFormat.Instance,
                ".gif" => GifFormat.Instance,
                ".bmp" => BmpFormat.Instance,
                _ => throw new Exception($"Unknown image format: {ext}")
            };

            thumb.Save(thumbStream, format);
            var thumbPath = Path.Combine(destinationPath, "thumb", thumbFileName).Replace("\\", "/");
            if (thumbPath.StartsWith("/"))
            {
                thumbPath = thumbPath.Substring(1);
            }

            await DoSaveAsync(thumbPath, thumbStream);

            return new StorageItemImageThumbnail(new Uri($"{_options.PublicUri}/{thumbPath}"), thumbPath, thumb.Width,
                thumb.Height, size.Key);
        }
    }
}
