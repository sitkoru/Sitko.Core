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
        private readonly ImgProxyBuilder _imgProxyBuilder;

        public ImgProxyUrlGenerator(IStorage<TStorageOptions> storage,
            StorageImgProxyModuleConfig<TStorageOptions> options,
            ILogger<ImgProxyUrlGenerator<TStorageOptions>> logger)
        {
            _storage = storage;
            _options = options;
            _logger = logger;
            _imgProxyBuilder = ImgProxyBuilder.New.WithEndpoint(options.Host)
                .WithCredentials(options.Key, options.Salt);
        }

        public string Resize(StorageItem item, int width, int height, string type = "auto", bool enlarge = false)
        {
            return _imgProxyBuilder.WithResize(type, width, height, enlarge)
                .Build(_storage.PublicUri(item).ToString(), _options.EncodeUrls);
        }
    }
}
