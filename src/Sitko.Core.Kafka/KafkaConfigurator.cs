using KafkaFlow;
using KafkaFlow.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sitko.Core.Kafka;

public class KafkaConfigurator
{
    private readonly string name;
    private readonly string[] brokers;

    internal KafkaConfigurator(string name, string[] brokers)
    {
        this.name = name;
        this.brokers = brokers;
    }

    private readonly List<Action<IConsumerConfigurationBuilder>> consumerActions = new();
    private readonly Dictionary<string, Action<IProducerConfigurationBuilder>> producerActions = new();

    public KafkaConfigurator AddProducer(string name, Action<IProducerConfigurationBuilder> configure)
    {
        producerActions[name] = configure;
        return this;
    }

    public KafkaConfigurator AddConsumer(Action<IConsumerConfigurationBuilder> configure)
    {
        consumerActions.Add(configure);
        return this;
    }

    public IServiceCollection Build(IServiceCollection serviceCollection)
    {
        serviceCollection.AddKafkaFlowHostedService(builder =>
        {
            builder
                .UseMicrosoftLog()
                .AddCluster(clusterBuilder =>
                {
                    clusterBuilder
                        .WithName(name)
                        .WithBrokers(brokers);
                    foreach (var (producerName, configure) in producerActions)
                    {
                        clusterBuilder.AddProducer(producerName, configurationBuilder =>
                        {
                            configure(configurationBuilder);
                        });
                    }

                    foreach (var consumerAction in consumerActions)
                    {
                        clusterBuilder.AddConsumer(consumerAction);
                    }
                });
        });
        return serviceCollection;
    }


}
