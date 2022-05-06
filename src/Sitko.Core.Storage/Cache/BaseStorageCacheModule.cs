using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Cache;

public abstract class
    BaseStorageCacheModule<TStorageOptions, TCache, TCacheOptions> : BaseApplicationModule<TCacheOptions>
    where TCacheOptions : StorageCacheOptions, new()
    where TCache : class, IStorageCache<TStorageOptions, TCacheOptions>
    where TStorageOptions : StorageOptions
{
    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        TCacheOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddSingleton<IStorageCache<TStorageOptions>, TCache>();
    }
}

public class
    FileStorageCacheModule<TStorageOptions> : BaseStorageCacheModule<TStorageOptions,
        FileStorageCache<TStorageOptions>,
        FileStorageCacheOptions> where TStorageOptions : StorageOptions
{
    public override string OptionsKey => $"Storage:Cache:FileSystem:{typeof(TStorageOptions).Name}";
}

public class
    InMemoryStorageCacheModule<TStorageOptions> : BaseStorageCacheModule<TStorageOptions,
        InMemoryStorageCache<TStorageOptions>,
        InMemoryStorageCacheOptions> where TStorageOptions : StorageOptions
{
    public override string OptionsKey => $"Storage:Cache:InMemory:{typeof(TStorageOptions).Name}";
}
