using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Storage.Metadata;

public abstract class
    BaseStorageMetadataModule<TStorageOptions, TProvider, TProviderOptions> : BaseApplicationModule<
    TProviderOptions> where TStorageOptions : StorageOptions
    where TProvider : class, IStorageMetadataProvider<TStorageOptions, TProviderOptions>
    where TProviderOptions : StorageMetadataModuleOptions<TStorageOptions>, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TProviderOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<IStorageMetadataProvider<TStorageOptions>, TProvider>();
        services.AddSingleton<TProvider>();
    }

    public override async Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await base.InitAsync(applicationContext, serviceProvider, cancellationToken);
        var metadataProvider = serviceProvider.GetRequiredService<TProvider>();
        await metadataProvider.InitAsync(cancellationToken);
    }

    public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
        TProviderOptions options) =>
        new[] { typeof(IStorageModule) };
}
