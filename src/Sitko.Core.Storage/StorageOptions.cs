using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.Storage.Cache;

namespace Sitko.Core.Storage
{
    public abstract class StorageOptions
    {
        public Uri? PublicUri { get; set; }

        public string? Prefix { get; set; }

        public TimeSpan StorageTreeCacheTimeout { get; set; } = TimeSpan.FromMinutes(30);

        public Action<IHostEnvironment, IConfiguration, IServiceCollection>? ConfigureCache { get; protected set; }

        public StorageOptions EnableCache<TCache, TCacheOptions>(Action<TCacheOptions>? configure = null)
            where TCache : class, IStorageCache<TCacheOptions> where TCacheOptions : StorageCacheOptions
        {
            ConfigureCache = (_, _, services) =>
            {
                var options = Activator.CreateInstance<TCacheOptions>();
                configure?.Invoke(options);
                services.AddSingleton(options);
                services.AddSingleton<IStorageCache, TCache>();
            };
            return this;
        }
    }
}
