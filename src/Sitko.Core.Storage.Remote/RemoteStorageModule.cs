using System;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Remote;

public class
    RemoteStorageModule<TStorageOptions> : StorageModule<RemoteStorage<TStorageOptions>, TStorageOptions>
    where TStorageOptions : StorageOptions, IRemoteStorageOptions, new()
{
    public override string OptionsKey => $"Storage:Remote:{typeof(TStorageOptions).Name}";

    public override void ConfigureServices(IApplicationContext context, IServiceCollection services,
        TStorageOptions startupOptions)
    {
        base.ConfigureServices(context, services, startupOptions);
        services.AddHttpClient<RemoteStorage<TStorageOptions>>();
        services.AddSingleton<IStorage<TStorageOptions>, RemoteStorage<TStorageOptions>>();
    }
}

public interface IRemoteStorageOptions
{
    public Uri RemoteUrl { get; }
}
