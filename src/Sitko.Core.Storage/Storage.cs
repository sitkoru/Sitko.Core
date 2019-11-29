using System;
using System.IO;
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

namespace Sitko.Core.Storage
{
    public abstract class Storage : IStorage
    {
        private readonly ILogger<Storage> _logger;
        private readonly StorageOptions _options;

        protected Storage(StorageOptions options, ILogger<Storage> logger)
        {
            _logger = logger;
            _options = options;
        }

        public async Task<StorageItem> SaveFileAsync(Stream file, string fileName, string path)
        {
            var destinationName = GetStorageFileName(fileName);
            var destinationPath = $"{path}/{destinationName}";


            var storageItem = new StorageItem
            {
                FileName = fileName,
                FileSize = file.Length,
                FilePath = destinationPath,
                Path = Path.GetDirectoryName(destinationPath)?.Replace("\\", "/"),
                PublicUri = new Uri($"{_options.PublicUri}/{destinationPath}")
            };

            if (_options.ProcessImages)
            {
                await TryProcessImageAsync(storageItem, file, path);
            }

            file.Seek(0, SeekOrigin.Begin);
            await DoSaveAsync(destinationPath, file);
            _logger.LogInformation("File saved to {path}", path);
            return storageItem;
        }

        protected abstract Task<bool> DoSaveAsync(string path, Stream file);


        public abstract Task<bool> DeleteFileAsync(string filePath);

        protected string GetStorageFileName(string fileName)
        {
            var extension = fileName.Substring(fileName.LastIndexOf('.'));
            return Guid.NewGuid() + extension;
        }

        private async Task TryProcessImageAsync(StorageItem storageItem, Stream file,
            string destinationPath)
        {
            try
            {
                file.Seek(0, SeekOrigin.Begin);
                using var image = Image.Load<Rgba32>(file);
                storageItem.Type = StorageItemType.Picture;
                storageItem.PictureInfo = new StorageItemPictureInfo
                {
                    VerticalResolution = image.Height,
                    HorizontalResolution = image.Width,
                    LargeThumbnail = await CreateThumbnailAsync(image,
                        _options.LargeThumbnailWidth,
                        _options.LargeThumbnailHeight, destinationPath, storageItem.StorageFileName),
                    MediumThumbnail = await CreateThumbnailAsync(image,
                        _options.MediumThumbnailWidth,
                        _options.MediumThumbnailHeight, destinationPath, storageItem.StorageFileName),
                    SmallThumbnail = await CreateThumbnailAsync(image,
                        _options.SmallThumbnailWidth,
                        _options.SmallThumbnailHeight, destinationPath, storageItem.StorageFileName)
                };
            }
            catch (Exception ex)
            {
                _logger.LogInformation("File is not image: {errorText}", ex.ToString());
            }
        }

        private async Task<StorageItemPictureThumbnail> CreateThumbnailAsync(Image<Rgba32> image, int maxWidth,
            int maxHeight, string destinationPath, string fileName)
        {
            var thumb = image.Clone();
            thumb.Mutate(i =>
                i.Resize(image.Width >= image.Height ? maxWidth : 0, image.Height > image.Width ? maxHeight : 0));
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

            return new StorageItemPictureThumbnail(new Uri($"{_options.PublicUri}/{thumbPath}"), thumbPath, thumb.Width,
                thumb.Height);
        }
    }
}
