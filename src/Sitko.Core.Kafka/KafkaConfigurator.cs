using Confluent.Kafka;
using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.Consumers.DistributionStrategies;
using Sitko.Core.App.Helpers;
using SecurityProtocol = KafkaFlow.Configuration.SecurityProtocol;

namespace Sitko.Core.Kafka;

public class KafkaConfigurator
{
    private readonly string clusterName;

    private readonly Dictionary<ConsumerRegistration, Action<IConsumerConfigurationBuilder, ConsumerConfig>>
        consumerActions = new();

    private readonly HashSet<ConsumerRegistration> consumers = [];
    private readonly Dictionary<string, Action<IProducerConfigurationBuilder, ProducerConfig>> producerActions = new();
    private readonly HashSet<string> topics = new();

    internal KafkaConfigurator(string clusterName) => this.clusterName = clusterName;

    internal HashSet<ConsumerRegistration> Consumers => consumers;
    internal ConsumerInitialState ConsumerInitialState { get; private set; } = ConsumerInitialState.Running;

    public KafkaConfigurator AddProducer(string producerName,
        Action<IProducerConfigurationBuilder, ProducerConfig> configure)
    {
        producerActions[producerName] = configure;
        return this;
    }

    public KafkaConfigurator AddConsumer(string consumerName, string groupId, TopicInfo[] consumerTopics,
        Action<IConsumerConfigurationBuilder, ConsumerConfig> configure)
    {
        var registration = new ConsumerRegistration(consumerName, groupId, consumerTopics);
        consumers.Add(registration);
        consumerActions[registration] = configure;
        return this;
    }

    public KafkaConfigurator AutoCreateTopic(string topic)
    {
        topics.Add(topic);
        return this;
    }

    public KafkaConfigurator WithConsumerState(ConsumerInitialState state = ConsumerInitialState.Running)
    {
        ConsumerInitialState = state;
        return this;
    }

    public void Build(IKafkaConfigurationBuilder builder, KafkaModuleOptions options) =>
        builder
            .UseMicrosoftLog()
            .AddOpenTelemetryInstrumentation()
            .AddCluster(clusterBuilder =>
            {
                clusterBuilder
                    .WithName(clusterName)
                    .WithBrokers(options.Brokers);
                if (options.UseSaslAuth)
                {
                    clusterBuilder
                        .WithSecurityInformation(information =>
                        {
                            information.SaslPassword = options.SaslPassword;
                            information.SaslUsername = options.SaslUserName;
                            information.SaslMechanism = options.SaslMechanisms;
                            information.SecurityProtocol = options.SecurityProtocol;
                            if (information.SecurityProtocol == SecurityProtocol.SaslSsl)
                            {
                                information.SslCaLocation = CertHelper.GetCertPath(options.SaslCertBase64);
                            }
                        });
                }

                if (!options.EnsureOffsets)
                {
                    foreach (var topic in topics)
                    {
                        clusterBuilder.CreateTopicIfNotExists(topic, options.TopicPartitionsCount,
                            options.TopicReplicationFactor, options.TopicConfigs);
                    }
                }

                foreach (var (producerName, configure) in producerActions)
                {
                    clusterBuilder.AddProducer(producerName, producerBuilder =>
                    {
                        var producerConfig = new ProducerConfig
                        {
                            ClientId = producerName,
                            MessageTimeoutMs = (int)options.MessageTimeout.TotalMilliseconds,
                            MessageMaxBytes = options.MessageMaxBytes,
                            EnableIdempotence = options.EnableIdempotence,
                            SocketNagleDisable = options.SocketNagleDisable,
                            Acks = options.Acks
                        };
                        producerBuilder.WithProducerConfig(producerConfig);
                        producerBuilder.WithLingerMs(options.MaxProducingTimeout.TotalMilliseconds);
                        configure(producerBuilder, producerConfig);
                    });
                }

                foreach (var (registration, configureAction) in consumerActions)
                {
                    var consumerTopics = registration.Topics.Select(info => info.Name).ToArray();
                    var consumerName =
                        $"{Environment.MachineName}/{registration.GroupId}/{string.Join('/', consumerTopics)}";
                    clusterBuilder.AddConsumer(consumerBuilder =>
                    {
                        consumerBuilder.WithName(consumerName);
                        consumerBuilder.Topics(consumerTopics);
                        consumerBuilder.WithGroupId(registration.GroupId);
                        consumerBuilder
                            .WithWorkerDistributionStrategy<BytesSumDistributionStrategy>(); // guarantee events order
                        consumerBuilder.WithMaxPollIntervalMs((int)options.MaxPollInterval.TotalMilliseconds);
                        var consumerConfig = new ConsumerConfig
                        {
                            MaxPartitionFetchBytes = options.MaxPartitionFetchBytes,
                            AutoOffsetReset = options.AutoOffsetReset,
                            ClientId = consumerName,
                            BootstrapServers = string.Join(",", options.Brokers),
                            SessionTimeoutMs = (int)options.SessionTimeout.TotalMilliseconds,
                            PartitionAssignmentStrategy = options.PartitionAssignmentStrategy
                        };
                        if (options is
                            {
                                PartitionAssignmentStrategy: PartitionAssignmentStrategy.CooperativeSticky,
                                EnableStaticMembership: true
                            })
                        {
                            consumerConfig.GroupInstanceId = consumerName;
                        }

                        consumerBuilder.WithConsumerConfig(consumerConfig);
                        configureAction(consumerBuilder, consumerConfig);
                        consumerBuilder.WithInitialState(ConsumerInitialState);
                    });
                }
            });
}

internal record ConsumerRegistration(string Name, string GroupId, TopicInfo[] Topics);

public record TopicInfo(string Name);
