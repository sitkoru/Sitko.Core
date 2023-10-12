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
        hostApplicationBuilder.AddSitkoCore().AddInMemoryQueue(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddInMemoryQueue(this IHostApplicationBuilder hostApplicationBuilder,
        Action<InMemoryQueueModuleOptions>? configure = null,
        string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddInMemoryQueue(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddInMemoryQueue(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, InMemoryQueueModuleOptions> configure,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<InMemoryQueueModule, InMemoryQueueModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddInMemoryQueue(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<InMemoryQueueModuleOptions>? configure = null,
        string? optionsKey = null) =>
        applicationBuilder.AddModule<InMemoryQueueModule, InMemoryQueueModuleOptions>(configure, optionsKey);
}
