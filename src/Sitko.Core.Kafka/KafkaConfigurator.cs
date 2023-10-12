using KafkaFlow;
using KafkaFlow.Configuration;

namespace Sitko.Core.Kafka;

public class KafkaConfigurator
{
    private readonly string[] brokers;

    private readonly List<Action<IConsumerConfigurationBuilder>> consumerActions = new();
    private readonly HashSet<ConsumerRegistration> consumers = new();
    private readonly string name;
    private readonly Dictionary<string, Action<IProducerConfigurationBuilder>> producerActions = new();
    private readonly Dictionary<string, (int Partitions, short ReplicationFactor)> topics = new();
    private bool ensureOffsets;

    internal KafkaConfigurator(string name, string[] brokers)
    {
        this.name = name;
        this.brokers = brokers;
    }

    internal string[] Brokers => brokers;
    internal HashSet<ConsumerRegistration> Consumers => consumers;
    internal bool NeedToEnsureOffsets => ensureOffsets;

    public KafkaConfigurator AddProducer(string producerName, Action<IProducerConfigurationBuilder> configure)
    {
        producerActions[producerName] = configure;
        return this;
    }

    public KafkaConfigurator AddConsumer(string consumerName, string groupId, TopicInfo[] topics,
        Action<IConsumerConfigurationBuilder> configure)
    {
        consumers.Add(new ConsumerRegistration(consumerName, groupId, topics));
        consumerActions.Add(configure);
        return this;
    }

    public KafkaConfigurator AutoCreateTopic(string topic, int partitions, short replicationFactor)
    {
        topics[topic] = (partitions, replicationFactor);
        return this;
    }

    public KafkaConfigurator EnsureOffsets(bool enable = true)
    {
        ensureOffsets = enable;
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
                if (!ensureOffsets)
                {
                    foreach (var (topic, config) in topics)
                    {
                        clusterBuilder.CreateTopicIfNotExists(topic, config.Partitions, config.ReplicationFactor);
                    }
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
                    clusterBuilder.AddConsumer(consumerBuilder =>
                    {
                        consumerAction(consumerBuilder);
                    });
                }
            });
}

internal record ConsumerRegistration(string Name, string GroupId, TopicInfo[] Topics);

public record TopicInfo(string Name, int PartitionsCount, short ReplicationFactor);
