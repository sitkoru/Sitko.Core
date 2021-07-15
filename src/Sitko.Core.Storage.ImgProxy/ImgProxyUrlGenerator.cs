using System;
using ImgProxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Storage.ImgProxy
{
    public class ImgProxyUrlGenerator<TStorageOptions> : IImgProxyUrlGenerator<TStorageOptions>
        where TStorageOptions : StorageOptions
    {
        private readonly IStorage<TStorageOptions> storage;
        private readonly IOptionsMonitor<ImgProxyStorageModuleOptions<TStorageOptions>> optionsMonitor;
        private ImgProxyStorageModuleOptions<TStorageOptions> Options => optionsMonitor.CurrentValue;
        private readonly ILogger<ImgProxyUrlGenerator<TStorageOptions>> logger;

        public ImgProxyUrlGenerator(IStorage<TStorageOptions> storage,
            IOptionsMonitor<ImgProxyStorageModuleOptions<TStorageOptions>> optionsMonitor,
            ILogger<ImgProxyUrlGenerator<TStorageOptions>> logger)
        {
            this.storage = storage;
            this.optionsMonitor = optionsMonitor;
            this.logger = logger;
        }

        private ImgProxyBuilder GetBuilder() =>
            ImgProxyBuilder.New.WithEndpoint(Options.Host)
                .WithCredentials(Options.Key, Options.Salt);

        public string Url(string url)
        {
            logger.LogDebug("Build url to image {Url}", url);
            return BuildUrl(url);
        }

        public string Format(string url, string format)
        {
            logger.LogDebug("Build url to image {Url} with format {Format}", url, format);
            return BuildUrl(url, builder => builder.WithFormat(format));
        }

        public string Preset(string url, string preset)
        {
            logger.LogDebug("Build url to image {Url} with preset {Preset}", url, preset);
            return BuildUrl(url, builder => builder.WithPreset(preset));
        }

        public string Build(string url, Action<ImgProxyBuilder> build)
        {
            logger.LogDebug("Build url to image {Url}", url);
            return BuildUrl(url, build);
        }

        public string Resize(string url, int width, int height, string type = "auto", bool enlarge = false)
        {
            logger.LogDebug(
                "Build url to resized image {Url}. Width: {Width}. Height: {Height}. Type: {Type}. Enlarge: {Enlarge}",
                url, width, height, type, enlarge);
            return BuildUrl(url, builder => builder.WithResize(type, width, height, enlarge));
        }

        public string Url(StorageItem item)
        {
            logger.LogDebug("Build url to item {Item}", item.FilePath);
            return BuildUrl(item);
        }

        public string Format(StorageItem item, string format)
        {
            logger.LogDebug("Build url to item {Item} with format {Format}", item.FilePath, format);
            return BuildUrl(item, builder => builder.WithFormat(format));
        }

        public string Preset(StorageItem item, string preset)
        {
            logger.LogDebug("Build url to item {Item} with preset {Preset}", item.FilePath, preset);
            return BuildUrl(item, builder => builder.WithPreset(preset));
        }

        public string Build(StorageItem item, Action<ImgProxyBuilder> build)
        {
            logger.LogDebug("Build url to item {Item}", item.FilePath);
            return BuildUrl(item, build);
        }

        public string Resize(StorageItem item, int width, int height, string type = "auto", bool enlarge = false)
        {
            logger.LogDebug(
                "Build url to resized item {Item}. Width: {Width}. Height: {Height}. Type: {Type}. Enlarge: {Enlarge}",
                item.FilePath, width, height, type, enlarge);
            return BuildUrl(item, builder => builder.WithResize(type, width, height, enlarge));
        }

        private string BuildUrl(StorageItem item, Action<ImgProxyBuilder>? build = null) =>
            BuildUrl(storage.PublicUri(item).ToString(), build);

        private string BuildUrl(string url, Action<ImgProxyBuilder>? build = null)
        {
            if (Options.DisableProxy)
            {
                return url;
            }

            var builder = GetBuilder();
            build?.Invoke(builder);
            return builder.Build(url, Options.EncodeUrls);
        }
    }
}
