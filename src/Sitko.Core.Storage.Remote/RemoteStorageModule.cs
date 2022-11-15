using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage.Remote;

public class
    RemoteStorageModule<TStorageOptions> : StorageModule<RemoteStorage<TStorageOptions>, TStorageOptions>
    where TStorageOptions : StorageOptions, IRemoteStorageOptions, new()
{
    public override string OptionsKey => $"Storage:Remote:{typeof(TStorageOptions).Name}";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TStorageOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddHttpClient<RemoteStorage<TStorageOptions>>();
        services
            .AddSingleton<IStorageMetadataProvider<TStorageOptions>, RemoteStorageMetadataProvider<TStorageOptions>>();
        services.AddSingleton<RemoteStorageMetadataProvider<TStorageOptions>>();
    }
}

public interface IRemoteStorageOptions
{
    public Uri RemoteUrl { get; }
    public Func<HttpClient>? HttpClientFactory { get; }
}

