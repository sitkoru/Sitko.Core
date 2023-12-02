using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sitko.Core.App;

namespace Sitko.Core.Storage;

public interface IStorageModule : IApplicationModule;

public abstract class StorageModule<TStorage, TStorageOptions> : BaseApplicationModule<TStorageOptions>,
    IStorageModule
    where TStorage : Storage<TStorageOptions> where TStorageOptions : StorageOptions, new()
{
    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TStorageOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.TryAddSingleton(provider =>
        {
            var instances = provider.GetServices<IStorageInstance>().OfType<IStorage>().ToArray();
            if (instances.Length == 0)
            {
                throw new InvalidOperationException("No storage instances registered");
            }

            var defaultInstances = instances.Where(i => i.IsDefault).ToArray();
            return defaultInstances.Length switch
            {
                <= 0 => instances.Last(),
                > 1 => throw new InvalidOperationException(
                    "Multiple storage instances registered as default. There should only be one."),
                _ => defaultInstances.First()
            };
        });
        services.TryAddSingleton<IEnumerable<IStorage>>(provider =>
            provider.GetServices<IStorageInstance>().OfType<IStorage>());
        services.AddSingleton<IStorageInstance, TStorage>();
        services.AddSingleton<IStorage<TStorageOptions>, TStorage>();
        services.AddSingleton<TStorage>();
    }
}

