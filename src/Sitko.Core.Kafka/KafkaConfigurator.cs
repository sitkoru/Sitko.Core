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
    private readonly Dictionary<string, (int Partitions, short ReplicationFactor)> topics = new();

    public KafkaConfigurator AddProducer(string producerName, Action<IProducerConfigurationBuilder> configure)
    {
        producerActions[producerName] = configure;
        return this;
    }

    public KafkaConfigurator AddConsumer(Action<IConsumerConfigurationBuilder> configure)
    {
        consumerActions.Add(configure);
        return this;
    }

    public KafkaConfigurator AutoCreateTopic(string topic, int partitions, short replicationFactor)
    {
        topics[topic] = (partitions, replicationFactor);
        return this;
    }

    public void Build(IKafkaConfigurationBuilder builder) =>
        builder
            .UseMicrosoftLog()
            .AddCluster(clusterBuilder =>
            {
                clusterBuilder
                    .WithName(name)
                    .WithBrokers(brokers);
                foreach (var (topic, config) in topics)
                {
                    clusterBuilder.CreateTopicIfNotExists(topic, config.Partitions, config.ReplicationFactor);
                }
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
}
