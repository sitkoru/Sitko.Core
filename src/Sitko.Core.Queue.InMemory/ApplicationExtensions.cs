using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Queue.InMemory;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddInMemoryQueue(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, InMemoryQueueModuleOptions> configure,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddInMemoryQueue(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddInMemoryQueue(this IHostApplicationBuilder hostApplicationBuilder,
        Action<InMemoryQueueModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.GetSitkoCore().AddInMemoryQueue(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static ISitkoCoreApplicationBuilder AddInMemoryQueue(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, InMemoryQueueModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<InMemoryQueueModule, InMemoryQueueModuleOptions>(configure, optionsKey);

    public static ISitkoCoreApplicationBuilder AddInMemoryQueue(this ISitkoCoreApplicationBuilder applicationBuilder,
        Action<InMemoryQueueModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<InMemoryQueueModule, InMemoryQueueModuleOptions>(configure, optionsKey);
}
