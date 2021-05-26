using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Cache
{
    public abstract class
        BaseStorageCacheModule<TStorageOptions, TCache, TCacheOptions> : BaseApplicationModule<TCacheOptions>
        where TCacheOptions : StorageCacheOptions, new()
        where TCache : class, IStorageCache<TStorageOptions, TCacheOptions>
        where TStorageOptions : StorageOptions
    {
        public BaseStorageCacheModule(Application application) : base(application)
        {
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton<IStorageCache<TStorageOptions>, TCache>();
        }
    }

    public class
        FileStorageCacheModule<TStorageOptions> : BaseStorageCacheModule<TStorageOptions,
            FileStorageCache<TStorageOptions>,
            FileStorageCacheOptions> where TStorageOptions : StorageOptions
    {
        public FileStorageCacheModule(Application application) : base(application)
        {
        }
    }
    
    public class
        InMemoryStorageCacheModule<TStorageOptions> : BaseStorageCacheModule<TStorageOptions,
            InMemoryStorageCache<TStorageOptions>,
            InMemoryStorageCacheOptions> where TStorageOptions : StorageOptions
    {
        public InMemoryStorageCacheModule(Application application) : base(application)
        {
        }
    }
}
