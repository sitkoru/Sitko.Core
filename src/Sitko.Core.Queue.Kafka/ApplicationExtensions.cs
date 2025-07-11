using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Kafka;

namespace Sitko.Core.Queue.Kafka;

[PublicAPI]
public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddKafkaQueue(
        this IHostApplicationBuilder applicationBuilder,
        Action<KafkaQueueModuleOptions> configure)
    {
        applicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>().AddKafkaQueue(configure);
        return applicationBuilder;
    }

    public static ISitkoCoreServerApplicationBuilder AddKafkaQueue(
        this ISitkoCoreServerApplicationBuilder applicationBuilder,
        Action<KafkaQueueModuleOptions> configure)
    {
        applicationBuilder.AddModule<KafkaQueueModule, KafkaQueueModuleOptions>(configure);
        if (!applicationBuilder.HasModule<KafkaModule>())
        {
            applicationBuilder.AddModule<KafkaModule, KafkaModuleOptions>();
        }

        return applicationBuilder;
    }
}
