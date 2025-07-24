using FluentValidation;
using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Kafka;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Execution;
using Sitko.Core.Tasks.Kafka.Execution;
using Sitko.Core.Tasks.Kafka.Scheduling;

namespace Sitko.Core.Tasks.Kafka;

public class
    KafkaTasksModule<TBaseTask, TDbContext> : TasksModule<TBaseTask, TDbContext, KafkaTaskScheduler,
        KafkaTasksModuleOptions<TBaseTask, TDbContext>> where TBaseTask : BaseTask
    where TDbContext : TasksDbContext<TBaseTask>
{
    public override string OptionsKey => $"Kafka:Tasks:{typeof(TBaseTask).Name}";

    protected override void ConfigureServicesInternal(IApplicationContext applicationContext,
        IServiceCollection services,
        KafkaTasksModuleOptions<TBaseTask, TDbContext> startupOptions, List<ExecutorRegistration> executors)
    {
        var kafkaTopicPrefix = startupOptions.AddTopicPrefix
            ? string.IsNullOrEmpty(startupOptions.TopicPrefix)
                ? $"{applicationContext.Name}_{applicationContext.Environment}"
                : startupOptions.TopicPrefix
            : "";
        var kafkaTopic = $"{kafkaTopicPrefix}_{startupOptions.TasksTopic}".Replace(".", "_");
        var kafkaGroupPrefix = startupOptions.AddConsumerGroupPrefix
            ? string.IsNullOrEmpty(startupOptions.ConsumerGroupPrefix)
                ? $"{applicationContext.Name}_{applicationContext.Environment}"
                : startupOptions.ConsumerGroupPrefix
            : "";

        var producerName = $"Tasks_{typeof(TBaseTask).Name}";
        foreach (var executor in executors)
        {
            EventsRegistry.Register(executor.EventType, kafkaTopic, producerName);
        }

        var kafkaConfigurator = applicationContext.GetModuleInstance<KafkaModule>()
            .CreateConfigurator("Kafka_Tasks_Cluster");
        kafkaConfigurator
            .AutoCreateTopic(kafkaTopic, startupOptions.TopicPartitions, startupOptions.TopicReplicationFactor)
            .EnsureOffsets()
            .AddProducer(producerName, (builder, _) =>
            {
                builder.DefaultTopic(kafkaTopic);
                builder.AddMiddlewares(middlewareBuilder =>
                    middlewareBuilder.AddSerializer<JsonCoreSerializer>());
            });
        var executorType = typeof(KafkaExecutor<,>);
        foreach (var groupConsumers in executors.GroupBy(r => r.GroupId))
        {
            var commonRegistration = groupConsumers.First();
            var name =
                $"{applicationContext.Name}/{applicationContext.Id}/{typeof(TBaseTask).Name}/{commonRegistration.GroupId}";
            var parallelThreadCount = groupConsumers.Max(r => r.ParallelThreadCount);
            var bufferSize = groupConsumers.Max(r => r.BufferSize);
            var groupId = $"{kafkaGroupPrefix}_{commonRegistration.GroupId}".Replace(".", "_");
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
                                handlers.AddHandlers(groupConsumers.Select(r =>
                                        executorType.MakeGenericType(r.EventType, r.ExecutorType)))
                                    .WithHandlerLifetime(InstanceLifetime.Scoped));
                        }
                    );
                });
        }
    }
}

public class
    KafkaModuleOptionsValidator<TBaseTask, TDbContext> : TasksModuleOptionsValidator<TBaseTask, TDbContext,
        KafkaTasksModuleOptions<TBaseTask, TDbContext>> where TBaseTask : BaseTask
    where TDbContext : TasksDbContext<TBaseTask>
{
    public KafkaModuleOptionsValidator() =>
        RuleFor(options => options.TasksTopic).NotEmpty().WithMessage("Specify Kafka topic");
}
