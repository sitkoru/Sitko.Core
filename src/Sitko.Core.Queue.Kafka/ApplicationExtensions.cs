using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Queue.Kafka;

public static class ApplicationExtensions
{
    public static IHostApplicationBuilder AddKafkaQueue(this IHostApplicationBuilder applicationBuilder,
        string clusterName, Action<KafkaConfigurator> configure, Action<KafkaModuleOptions>? configureKafkaAction = null)
    {
        applicationBuilder.GetSitkoCore<ISitkoCoreServerApplicationBuilder>()
            .AddKafkaQueue(clusterName, configure, configureKafkaAction);
        return applicationBuilder;
    }

    public static ISitkoCoreServerApplicationBuilder AddKafkaQueue(this ISitkoCoreServerApplicationBuilder applicationBuilder,
        string clusterName, Action<KafkaConfigurator> configure, Action<KafkaModuleOptions>? configureKafkaAction = null)
    {
        configure(KafkaModule.CreateConfigurator(clusterName));
        applicationBuilder.AddModule<KafkaModule, KafkaModuleOptions>(configureKafkaAction);
        return applicationBuilder;
    }
}
