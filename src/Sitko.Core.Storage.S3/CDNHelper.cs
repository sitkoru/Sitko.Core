using System;
using JetBrains.Annotations;

namespace Sitko.Core.Storage.S3
{
    [UsedImplicitly]
    public class CdnHelper<T> where T : IS3StorageOptions
    {
        private readonly T _options;

        public CdnHelper(T options)
        {
            _options = options;
        }

        public string GetCdnUrl(string url, Func<T, string> path)
        {
            return $"{_options.PublicUri}{path(_options)}/{url}";
        }
    }
}
