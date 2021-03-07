using System;
using ImgProxy;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Storage.ImgProxy
{
    public class ImgProxyUrlGenerator<TStorageOptions> : IImgProxyUrlGenerator<TStorageOptions>
        where TStorageOptions : StorageOptions
    {
        private readonly IStorage<TStorageOptions> _storage;
        private readonly StorageImgProxyModuleConfig<TStorageOptions> _options;
        private readonly ILogger<ImgProxyUrlGenerator<TStorageOptions>> _logger;

        public ImgProxyUrlGenerator(IStorage<TStorageOptions> storage,
            StorageImgProxyModuleConfig<TStorageOptions> options,
            ILogger<ImgProxyUrlGenerator<TStorageOptions>> logger)
        {
            _storage = storage;
            _options = options;
            _logger = logger;
        }

        private ImgProxyBuilder GetBuilder()
        {
            return ImgProxyBuilder.New.WithEndpoint(_options.Host)
                .WithCredentials(_options.Key, _options.Salt);
        }

        public string Url(StorageItem item)
        {
            _logger.LogDebug("Build url to item {Item}", item.FilePath);
            return BuildUrl(item);
        }

        public string Format(StorageItem item, string format)
        {
            _logger.LogDebug("Build url to item {Item} with format {Format}", item.FilePath, format);
            return BuildUrl(item, builder => builder.WithFormat(format));
        }

        public string Preset(StorageItem item, string preset)
        {
            _logger.LogDebug("Build url to item {Item} with preset {Preset}", item.FilePath, preset);
            return BuildUrl(item, builder => builder.WithPreset(preset));
        }

        public string Build(StorageItem item, Action<ImgProxyBuilder> build)
        {
            _logger.LogDebug("Build url to item {Item}", item.FilePath);
            return BuildUrl(item, build);
        }

        public string Resize(StorageItem item, int width, int height, string type = "auto", bool enlarge = false)
        {
            _logger.LogDebug(
                "Build url to resized item {Item}. Width: {Width}. Height: {Height}. Type: {Type}. Enlarge: {Enlarge}",
                item.FilePath, width, height, type, enlarge);
            return BuildUrl(item, builder => builder.WithResize(type, width, height, enlarge));
        }

        private string BuildUrl(StorageItem item, Action<ImgProxyBuilder>? build = null)
        {
            var builder = GetBuilder();
            build?.Invoke(builder);
            return builder.Build(_storage.PublicUri(item).ToString(), _options.EncodeUrls);
        }
    }
}
