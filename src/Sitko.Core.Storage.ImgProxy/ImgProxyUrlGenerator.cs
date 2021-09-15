using System;
using ImgProxy;
using Microsoft.Extensions.Logging;
using Sitko.Core.ImgProxy;

namespace Sitko.Core.Storage.ImgProxy
{
    public class ImgProxyUrlGenerator<TStorageOptions> : IImgProxyUrlGenerator<TStorageOptions>
        where TStorageOptions : StorageOptions
    {
        private readonly IImgProxyUrlGenerator imgProxyUrlGenerator;
        private readonly IStorage<TStorageOptions> storage;
        private readonly ILogger<ImgProxyUrlGenerator<TStorageOptions>> logger;

        public ImgProxyUrlGenerator(IImgProxyUrlGenerator imgProxyUrlGenerator, IStorage<TStorageOptions> storage,
            ILogger<ImgProxyUrlGenerator<TStorageOptions>> logger)
        {
            this.imgProxyUrlGenerator = imgProxyUrlGenerator;
            this.storage = storage;
            this.logger = logger;
        }

        public string Url(StorageItem item)
        {
            logger.LogDebug("Build url to item {Item}", item.FilePath);
            return imgProxyUrlGenerator.Url(storage.PublicUri(item).ToString());
        }

        public string Format(StorageItem item, string format)
        {
            logger.LogDebug("Build url to item {Item} with format {Format}", item.FilePath, format);
            return imgProxyUrlGenerator.Format(storage.PublicUri(item).ToString(), format);
        }

        public string Preset(StorageItem item, string preset)
        {
            logger.LogDebug("Build url to item {Item} with preset {Preset}", item.FilePath, preset);
            return imgProxyUrlGenerator.Preset(storage.PublicUri(item).ToString(), preset);
        }

        public string Build(StorageItem item, Action<ImgProxyBuilder> build)
        {
            logger.LogDebug("Build url to item {Item}", item.FilePath);
            return imgProxyUrlGenerator.Build(storage.PublicUri(item).ToString(), build);
        }

        public string Resize(StorageItem item, int width, int height, string type = "auto", bool enlarge = false,
            bool extend = false)
        {
            logger.LogDebug(
                "Build url to resized item {Item}. Width: {Width}. Height: {Height}. Type: {Type}. Enlarge: {Enlarge}",
                item.FilePath, width, height, type, enlarge);
            return imgProxyUrlGenerator.Resize(storage.PublicUri(item).ToString(), width, height, type, enlarge,
                extend);
        }

        public string Url(string url) => imgProxyUrlGenerator.Url(url);

        public string Format(string url, string format) => imgProxyUrlGenerator.Format(url, format);

        public string Preset(string url, string preset) => imgProxyUrlGenerator.Preset(url, preset);

        public string Build(string url, Action<ImgProxyBuilder> build) => imgProxyUrlGenerator.Build(url, build);

        public string Resize(string url, int width, int height, string type = "auto", bool enlarge = false,
            bool extend = false) => imgProxyUrlGenerator.Resize(url, width, height, type, enlarge, extend);
    }
}
