using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Kafka;

public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddKafkaQueue(this IHostApplicationBuilder applicationBuilder,
        Action<KafkaModuleOptions>? configureKafkaAction = null)
    {
        applicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>()
            .AddKafkaQueue(configureKafkaAction);
        return applicationBuilder;
    }

    public static ISitkoCoreServerApplicationBuilder AddKafkaQueue(this ISitkoCoreServerApplicationBuilder applicationBuilder,
        Action<KafkaModuleOptions>? configureKafkaAction = null)
    {
        applicationBuilder.AddModule<KafkaModule, KafkaModuleOptions>(configureKafkaAction);
        return applicationBuilder;
    }
}
