using Confluent.Kafka;
using KafkaFlow;
using KafkaFlow.Configuration;
using KafkaFlow.Consumers.DistributionStrategies;
using KafkaFlow.Serializer;
using Sitko.Core.App;
using Sitko.Core.App.Helpers;
using Sitko.Core.Queue.Kafka.Attributes;
using Sitko.Core.Queue.Kafka.Events;
using Sitko.Core.Queue.Kafka.Middleware.Consumption;
using Sitko.Core.Queue.Kafka.Middleware.Producing;
using SecurityProtocol = KafkaFlow.Configuration.SecurityProtocol;

namespace Sitko.Core.Queue.Kafka;

public class KafkaConfigurator
{
    private readonly string clusterName;
    private readonly HashSet<ConsumerRegistration> consumers = new();
    private readonly HashSet<ProducerRegistration> producers = new();
    private readonly HashSet<Action<IConsumerMiddlewareConfigurationBuilder>> consumerMiddlewares = new();
    private readonly HashSet<Action<IProducerMiddlewareConfigurationBuilder>> producerMiddlewares = new();
    private readonly Dictionary<string, (int Partitions, short ReplicationFactor)> topics = new();

    private bool ensureOffsets;

    internal KafkaConfigurator(string clusterName) => this.clusterName = clusterName;

    internal HashSet<ConsumerRegistration> Consumers => consumers;
    internal bool NeedToEnsureOffsets => ensureOffsets;

    public KafkaConfigurator AddProducer(string producerName, string defaultTopic)
    {
        producers.Add(new ProducerRegistration(producerName, defaultTopic));
        return this;
    }

    public KafkaConfigurator AddProducerMiddlewares(Action<IProducerMiddlewareConfigurationBuilder> middleware)
    {
        producerMiddlewares.Add(middleware);
        return this;
    }

    public KafkaConfigurator AddConsumer(Type consumerType, IApplicationContext applicationContext, TopicInfo[] consumerTopics, string groupPrefix = "")
    {
        var attribute = consumerType.FindAttribute<MessageHandlerAttribute>();
        if (attribute == null)
        {
            return this;
        }
        var eventType = consumerType.BaseType is { IsGenericType: true } ?
            consumerType.BaseType.GenericTypeArguments
                .FirstOrDefault()?.Name ?? nameof(BaseEvent) :
            nameof(BaseEvent);

        var name =
            $"{applicationContext.Name}/{applicationContext.Id}/{eventType}/{attribute.GroupId}";
        var groupId = !string.IsNullOrEmpty(groupPrefix) ? $"{groupPrefix}_{attribute.GroupId}" : attribute.GroupId;
        consumers.Add(new ConsumerRegistration(name, groupId.Replace(".", "_"), consumerTopics, consumerType, attribute));

        return this;
    }

    public KafkaConfigurator AddConsumer<TConsumer>(IApplicationContext applicationContext, TopicInfo[] consumerTopics, string groupPrefix = "")
    {
        AddConsumer(typeof(TConsumer), applicationContext, consumerTopics, groupPrefix);
        return this;
    }

    public KafkaConfigurator AddConsumerMiddlewares(Action<IConsumerMiddlewareConfigurationBuilder> middleware)
    {
        consumerMiddlewares.Add(middleware);
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

    public KafkaConfigurator RegisterEvent<TEvent>(string topic, string producerName)
    {
        RegisterEvent(typeof(TEvent), topic, producerName);
        return this;
    }

    public KafkaConfigurator RegisterEvent(Type eventType, string topic, string producerName)
    {
        EventsRegistry.Register(eventType, topic, producerName);
        return this;
    }

    public void Build(IKafkaConfigurationBuilder builder, KafkaModuleOptions options) =>
        builder
            .UseMicrosoftLog()
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
                if (!ensureOffsets)
                {
                    foreach (var (topic, config) in topics)
                    {
                        clusterBuilder.CreateTopicIfNotExists(topic, config.Partitions, config.ReplicationFactor);
                    }
                }

                foreach (var producer in producers)
                {
                    clusterBuilder.AddProducer(producer.Name, producerBuilder =>
                    {
                        var producerConfig = new ProducerConfig
                        {
                            ClientId = producer.Name,
                            MessageTimeoutMs = (int)options.MessageTimeout.TotalMilliseconds,
                            MessageMaxBytes = options.MessageMaxBytes,
                            EnableIdempotence = options.EnableIdempotence,
                            SocketNagleDisable = options.SocketNagleDisable,
                            Acks = options.Acks
                        };
                        producerBuilder.WithProducerConfig(producerConfig);
                        producerBuilder.WithLingerMs(options.MaxProducingTimeout.TotalMilliseconds);
                        producerBuilder.DefaultTopic(producer.DefaultTopic);
                        producerBuilder.AddMiddlewares(middlewares =>
                        {
                            middlewares.Add<EventsProducingLogger>();
                            middlewares.AddSerializer<JsonCoreSerializer>();
                            foreach (var middleware in producerMiddlewares)
                            {
                                middleware(middlewares);
                            }
                        });
                    });
                }

                foreach (var consumersGroup in consumers.GroupBy(c => c.Attribute.GroupId))
                {
                    var consumer = consumersGroup.First();
                    clusterBuilder.AddConsumer(consumerBuilder =>
                    {
                        consumerBuilder.WithName(consumer.Name);
                        consumerBuilder.Topics(consumer.Topics.Select(info => info.Name));
                        consumerBuilder.WithGroupId(consumer.GroupId);
                        consumerBuilder
                            .WithWorkerDistributionStrategy<BytesSumDistributionStrategy>(); // guarantee events order
                        consumerBuilder.WithMaxPollIntervalMs((int)options.MaxPollInterval.TotalMilliseconds);
                        var consumerConfig = new ConsumerConfig
                        {
                            MaxPartitionFetchBytes = options.MaxPartitionFetchBytes,
                            AutoOffsetReset = options.AutoOffsetReset,
                            ClientId = consumer.Name,
                            // GroupInstanceId = registration.Name, // TODO: Try after https://github.com/Farfetch/kafkaflow/issues/456
                            BootstrapServers = string.Join(",", options.Brokers),
                            SessionTimeoutMs = (int)options.SessionTimeout.TotalMilliseconds,
                            PartitionAssignmentStrategy = options.PartitionAssignmentStrategy
                        };
                        consumerBuilder.WithConsumerConfig(consumerConfig);
                        consumerBuilder.WithWorkersCount(consumer.Attribute.ParallelThreadCount);
                        consumerBuilder.WithBufferSize(consumer.Attribute.BufferSize);
                        consumerBuilder.AddMiddlewares(middlewares =>
                        {
                            middlewares.Add<ConsumptionDelayMiddleware>();
                            middlewares.Add<EventConsumptionLogger>();
                            middlewares.AddDeserializer<JsonCoreDeserializer>();
                            middlewares.AddTypedHandlers(handlers =>
                                handlers.AddHandlers(consumersGroup.Select(c => c.Type))
                                    .WithHandlerLifetime(InstanceLifetime.Scoped));
                            foreach (var middleware in consumerMiddlewares)
                            {
                                middleware(middlewares);
                            }
                        });
                    });
                }
            });
}

internal record ConsumerRegistration(string Name, string GroupId, TopicInfo[] Topics, Type Type, MessageHandlerAttribute Attribute);

internal record ProducerRegistration(string Name, string DefaultTopic);

public record TopicInfo(string Name, int PartitionsCount, short ReplicationFactor);
