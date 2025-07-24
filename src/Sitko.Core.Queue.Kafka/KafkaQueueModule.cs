using KafkaFlow;
using KafkaFlow.Retry;
using KafkaFlow.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Kafka;
using Sitko.Core.Kafka.Middleware.Consuming;
using Sitko.Core.Kafka.Middleware.Producing;

namespace Sitko.Core.Queue.Kafka;

public class KafkaQueueModule : BaseApplicationModule<KafkaQueueModuleOptions>
{
    public override string OptionsKey => "Kafka:Queue";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        KafkaQueueModuleOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);

        Dictionary<Type, EventMetadata> events = new();
        var eventTypes = startupOptions.Assemblies.SelectMany(assembly => assembly.ExportedTypes.Where(type =>
            !type.IsAbstract && type.GetCustomAttributes(typeof(QueueEventAttribute), true).Length != 0)).ToList();

        foreach (var eventType in eventTypes)
        {
            var queueAttribute = eventType.GetCustomAttributes(typeof(QueueEventAttribute), true)
                .Cast<QueueEventAttribute>().First();
            var topic = queueAttribute.Topic;

            var eventMetadata = new EventMetadata(
                eventType,
                queueAttribute.EventTypeId,
                startupOptions.GetPrefixedTopicName(topic),
                topic
            );
            events[eventType] = eventMetadata;
        }

        HashSet<ConsumerRegistration> consumers = [];
        var eventHandlers = startupOptions.Assemblies.SelectMany(assembly => assembly.ExportedTypes.Where(type =>
            !type.IsAbstract && type.GetCustomAttributes(typeof(QueueHandlerAttribute), true).Length != 0 &&
            typeof(IMessageHandler).IsAssignableFrom(type))).ToList();
        foreach (var eventHandler in eventHandlers)
        {
            var queueHandlerAttribute = eventHandler.GetCustomAttributes(typeof(QueueHandlerAttribute), true)
                .Cast<QueueHandlerAttribute>().First();

            var eventType = eventHandler.GetInterfaces()
                .First(i => i.IsGenericType && typeof(IMessageHandler).IsAssignableFrom(i)).GenericTypeArguments
                .First();
            var eventMetadata = events[eventType];

            consumers.Add(
                new ConsumerRegistration(
                    eventHandler,
                    eventType,
                    eventMetadata.PrefixedTopicName,
                    eventMetadata.Topic,
                    startupOptions.GetPrefixedGroupName(queueHandlerAttribute.Group),
                    queueHandlerAttribute.Group,
                    queueHandlerAttribute.ParallelThreadCount,
                    queueHandlerAttribute.BufferSize,
                    queueHandlerAttribute.RetryStrategy
                )
            );
        }

        HashSet<BatchConsumerRegistration> batchConsumers = [];
        var batchEventHandlers = startupOptions.Assemblies.SelectMany(assembly => assembly.ExportedTypes.Where(type =>
            !type.IsAbstract && type.GetCustomAttributes(typeof(QueueBatchHandlerAttribute), true).Length != 0 &&
            typeof(IBatchMessageHandler).IsAssignableFrom(type))).ToList();
        foreach (var batchEventHandler in batchEventHandlers)
        {
            var queueBatchHandlerAttribute = batchEventHandler
                .GetCustomAttributes(typeof(QueueBatchHandlerAttribute), true)
                .Cast<QueueBatchHandlerAttribute>().First();

            var eventType = batchEventHandler.GetInterfaces()
                .First(i => i.IsGenericType && typeof(IBatchMessageHandler).IsAssignableFrom(i)).GenericTypeArguments
                .First();
            var eventMetadata = events[eventType];

            if (consumers.Any(registration => registration.GroupName == queueBatchHandlerAttribute.Group))
            {
                throw new InvalidOperationException(
                    $"Can't add batch consumer {batchEventHandler} into existing group. Group: " +
                    queueBatchHandlerAttribute.Group);
            }

            batchConsumers.Add(
                new BatchConsumerRegistration(
                    batchEventHandler,
                    eventType,
                    eventMetadata.PrefixedTopicName,
                    eventMetadata.Topic,
                    startupOptions.GetPrefixedGroupName(queueBatchHandlerAttribute.Group),
                    queueBatchHandlerAttribute.Group,
                    queueBatchHandlerAttribute.ParallelThreadCount,
                    queueBatchHandlerAttribute.BufferSize,
                    queueBatchHandlerAttribute.BatchSize,
                    TimeSpan.FromSeconds(queueBatchHandlerAttribute.BatchTimeoutInSeconds)
                )
            );
            services.AddScoped(batchEventHandler);
        }

        services.AddSingleton(new KafkaMetadata(events));
        services.AddScoped<IEventProducer, EventProducer>();
        services.AddSingleton<IKafkaQueueController, KafkaQueueController>();

        var kafkaConfigurator = applicationContext.GetModuleInstance<KafkaModule>()
            .CreateConfigurator(startupOptions.ClusterName);
        foreach (var topicName in events.Values.Select(x => x.PrefixedTopicName).Distinct())
        {
            kafkaConfigurator.AutoCreateTopic(topicName, startupOptions.PartitionsCount,
                startupOptions.ReplicationFactor);
        }

        kafkaConfigurator.AddProducer(startupOptions.ProducerName, (builder, _) =>
        {
            builder.AddMiddlewares(middlewareBuilder =>
            {
                middlewareBuilder.Add<ProducingTelemetryMiddleware>();
                middlewareBuilder.AddSerializer<JsonCoreSerializer, EventTypeIdTypeResolver>();
            });
        });

        foreach (var topicConsumers in consumers.GroupBy(r => r.TopicName))
        {
            foreach (var topicConsumersGroup in topicConsumers.GroupBy(r => r.GroupName))
            {
                AddConsumer(kafkaConfigurator, topicConsumersGroup.ToList(), startupOptions);
            }
        }

        foreach (var topicConsumers in batchConsumers.GroupBy(r => r.TopicName))
        {
            foreach (var topicConsumersGroup in topicConsumers.GroupBy(r => r.GroupName))
            {
                if (topicConsumersGroup.Count() > 1)
                {
                    throw new InvalidOperationException("Can't add more than one batch consumer to group");
                }

                AddBatchConsumer(kafkaConfigurator, topicConsumersGroup.First(), startupOptions);
            }
        }
    }

    private static void AddBatchConsumer(KafkaConfigurator kafkaConfigurator, BatchConsumerRegistration consumer,
        KafkaQueueModuleOptions options)
    {
        var consumerName =
            $"{Environment.MachineName}/{consumer.PrefixedGroupName}/{consumer.PrefixedTopicName}";
        var bufferSize = consumer.BufferSize;
        kafkaConfigurator.AddConsumer(consumerName, consumer.PrefixedGroupName,
            [new TopicInfo(consumer.PrefixedTopicName, options.PartitionsCount, options.ReplicationFactor)],
            (consumerBuilder, _) =>
            {
                consumerBuilder.WithWorkersCount(1);
                consumerBuilder.WithBufferSize(bufferSize);
                consumerBuilder.AddMiddlewares(middlewares =>
                    {
                        middlewares.AddDeserializer<JsonCoreDeserializer, EventTypeIdTypeResolver>();
                        middlewares.AddBatching(consumer.BatchSize, consumer.BatchTimeout);
                        middlewares.Add(factory =>
                            factory.Resolve(consumer.EventHandler) as IMessageMiddleware);
                    }
                );
                if (!options.StartConsumers)
                {
                    consumerBuilder.WithInitialState(ConsumerInitialState.Stopped);
                }
            });
    }

    private static void AddConsumer(KafkaConfigurator kafkaConfigurator, List<ConsumerRegistration> groupConsumers,
        KafkaQueueModuleOptions options)
    {
        var commonRegistration = groupConsumers.First();
        var consumerName =
            $"{Environment.MachineName}/{commonRegistration.PrefixedGroupName}/{commonRegistration.PrefixedTopicName}";
        var parallelThreadCount = groupConsumers.Max(r => r.ParallelThreadCount);
        var bufferSize = groupConsumers.Max(r => r.BufferSize);
        var handlerTypes = groupConsumers.Select(r => r.EventHandler).ToArray();
        kafkaConfigurator.AddConsumer(consumerName, commonRegistration.PrefixedGroupName,
            [new TopicInfo(commonRegistration.PrefixedTopicName, options.PartitionsCount, options.ReplicationFactor)],
            (consumerBuilder, _) =>
            {
                consumerBuilder.WithWorkersCount(parallelThreadCount);
                consumerBuilder.WithBufferSize(bufferSize);
                consumerBuilder.AddMiddlewares(middlewares =>
                    {
                        switch (commonRegistration.RetryStrategy)
                        {
                            case ConsumerGroupRetryStrategy.Simple:
                                middlewares.RetrySimple(builder => builder.HandleAnyException()
                                    .TryTimes(options.SimpleRetryCount)
                                    .WithTimeBetweenTriesPlan(options.SimpleRetryIntervals));
                                break;
                            case ConsumerGroupRetryStrategy.None:
                            case ConsumerGroupRetryStrategy.Forever:
                            case ConsumerGroupRetryStrategy.DurableOrdered:
                            case ConsumerGroupRetryStrategy.DurableLatest:
                            default:
                                // TODO: implement
                                break;
                        }

                        middlewares.AddDeserializer<JsonCoreDeserializer, EventTypeIdTypeResolver>();
                        middlewares.Add<ConsumingTelemetryMiddleware>();
                        middlewares.AddTypedHandlers(handlers =>
                            handlers.AddHandlers(handlerTypes).WithHandlerLifetime(InstanceLifetime.Scoped));
                    }
                );
                if (!options.StartConsumers)
                {
                    consumerBuilder.WithInitialState(ConsumerInitialState.Stopped);
                }
            });
    }
}
