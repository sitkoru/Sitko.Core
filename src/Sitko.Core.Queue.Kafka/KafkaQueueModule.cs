using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Kafka;

namespace Sitko.Core.Queue.Kafka;

public class KafkaQueueModule : BaseApplicationModule<KafkaQueueModuleOptions>
{
    public override string OptionsKey => "Kafka:Queue";

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        KafkaQueueModuleOptions startupOptions)
    {
        services.AddScoped<KafkaQueue>();

        var kafkaTopicPrefix = startupOptions.AddTopicPrefix
            ? string.IsNullOrEmpty(startupOptions.TopicPrefix)
                ? $"{applicationContext.Name}_{applicationContext.Environment}"
                : startupOptions.TopicPrefix
            : "";
        var kafkaTopic = $"{kafkaTopicPrefix}_{startupOptions.QueueTopic}".Replace(".", "_");
        var kafkaGroupPrefix = startupOptions.AddConsumerGroupPrefix
            ? string.IsNullOrEmpty(startupOptions.ConsumerGroupPrefix)
                ? $"{applicationContext.Name}_{applicationContext.Environment}"
                : startupOptions.ConsumerGroupPrefix
            : "";

        var kafkaConfigurator = KafkaModule.CreateConfigurator("Kafka_Queue_Cluster");
        kafkaConfigurator
            .AutoCreateTopic(kafkaTopic, startupOptions.TopicPartitions, startupOptions.TopicReplicationFactor)
            .EnsureOffsets();

        if (startupOptions.Messages.Count > 0)
        {
            foreach (var producerName in startupOptions.Messages.Select(KafkaQueueHelper.GetProducerName))
            {
                kafkaConfigurator.AddProducer(producerName, (builder, _) =>
                {
                    builder.DefaultTopic(kafkaTopic);
                    builder.AddMiddlewares(middlewareBuilder =>
                        middlewareBuilder.AddSerializer<JsonCoreSerializer>());
                });
            }
        }

        if (startupOptions.Consumers.Count == 0)
        {
            return;
        }

        foreach (var consumersGroup in  KafkaQueueHelper
                     .GetConsumersWithAttributes(startupOptions.Consumers)
                     .GroupBy(c => c.Value.GroupId))
        {
            var name =
                $"{applicationContext.Name}/{applicationContext.Id}/{consumersGroup.Key}";
            var groupId = $"{kafkaGroupPrefix}_{consumersGroup.Key}".Replace(".", "_");
            var parallelThreadCount = consumersGroup.Max(r => r.Value.ParallelThreadCount);
            var bufferSize = consumersGroup.Max(r => r.Value.BufferSize);
            kafkaConfigurator.AddConsumer(name, groupId,
            new[]
            {
                new TopicInfo(kafkaTopic, startupOptions.TopicPartitions, startupOptions.TopicReplicationFactor)
            }, (consumerBuilder, _) =>
            {
                consumerBuilder.WithWorkersCount(parallelThreadCount);
                consumerBuilder.WithBufferSize(bufferSize);
                consumerBuilder.AddMiddlewares(
                middlewares =>
                {
                    middlewares.AddDeserializer<JsonCoreDeserializer>();
                    middlewares.AddTypedHandlers(handlers =>
                        handlers.AddHandlers(consumersGroup.Select(g => g.Key))
                            .WithHandlerLifetime(InstanceLifetime.Scoped));
                }
                );
            });
        }
    }
}
