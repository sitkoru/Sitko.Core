using System;
using JetBrains.Annotations;

namespace Sitko.Core.Storage
{
    [UsedImplicitly]
    public class CdnHelper
    {
        private readonly S3StorageOptions _options;

        public CdnHelper(S3StorageOptions options)
        {
            _options = options;
        }

        public string GetCdnUrl(string url, Func<S3StorageOptions, string> path)
        {
            return $"{_options.PublicUri}{path(_options)}/{url}";
        }
    }
}
