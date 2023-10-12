using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Nats;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddNatsQueue(this IHostApplicationBuilder hostApplicationBuilder,
        Action<IApplicationContext, NatsQueueModuleOptions> configure, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddNatsQueue(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static IHostApplicationBuilder AddNatsQueue(this IHostApplicationBuilder hostApplicationBuilder,
        Action<NatsQueueModuleOptions>? configure = null, string? optionsKey = null)
    {
        hostApplicationBuilder.AddSitkoCore().AddNatsQueue(configure, optionsKey);
        return hostApplicationBuilder;
    }

    public static SitkoCoreApplicationBuilder AddNatsQueue(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<IApplicationContext, NatsQueueModuleOptions> configure, string? optionsKey = null) =>
        applicationBuilder.AddModule<NatsQueueModule, NatsQueueModuleOptions>(configure, optionsKey);

    public static SitkoCoreApplicationBuilder AddNatsQueue(this SitkoCoreApplicationBuilder applicationBuilder,
        Action<NatsQueueModuleOptions>? configure = null, string? optionsKey = null) =>
        applicationBuilder.AddModule<NatsQueueModule, NatsQueueModuleOptions>(configure, optionsKey);
}
