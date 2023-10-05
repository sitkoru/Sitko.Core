using Confluent.Kafka;
using FluentValidation;
using KafkaFlow;
using KafkaFlow.Consumers.DistributionStrategies;
using KafkaFlow.Serializer;
using KafkaFlow.TypedHandler;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Execution;
using Sitko.Core.Tasks.Kafka.Execution;
using Sitko.Core.Tasks.Kafka.Scheduling;
using Sitko.Core.Tasks.Scheduling;
using AutoOffsetReset = Confluent.Kafka.AutoOffsetReset;

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
            ? (string.IsNullOrEmpty(startupOptions.TopicPrefix)
                ? $"{applicationContext.Name}_{applicationContext.Environment}"
                : startupOptions.TopicPrefix)
            : "";
        var kafkaTopic = $"{kafkaTopicPrefix}_{startupOptions.TasksTopic}";
        services.AddKafkaFlowHostedService(builder =>
        {
            builder
                .UseMicrosoftLog()
                .AddCluster(clusterBuilder =>
                {
                    clusterBuilder
                        .WithName($"Tasks_{typeof(TBaseTask).Name}")
                        .WithBrokers(startupOptions.Brokers)
                        .AddProducer("default", producerBuilder =>
                        {
                            producerBuilder
                                .DefaultTopic(kafkaTopic)
                                .AddMiddlewares(configurationBuilder =>
                                    configurationBuilder.AddSerializer<JsonCoreSerializer>());
                        });
                    // регистрируем консьюмеры на каждую группу экзекьюторов
                    var executorType = typeof(KafkaExecutor<,>);
                    foreach (var groupConsumers in executors.GroupBy(r => r.GroupId))
                    {
                        var commonRegistration = groupConsumers.First();
                        var name =
                            $"{applicationContext.Name}/{applicationContext.Id}/{commonRegistration.GroupId}";

                        var parallelThreadCount = groupConsumers.Max(r => r.ParallelThreadCount);
                        var bufferSize = groupConsumers.Max(r => r.BufferSize);

                        clusterBuilder.AddConsumer(
                            consumerBuilder =>
                            {
                                consumerBuilder.Topic(kafkaTopic);
                                consumerBuilder.WithName(name);
                                consumerBuilder.WithGroupId(commonRegistration.GroupId);
                                consumerBuilder.WithWorkersCount(parallelThreadCount);
                                consumerBuilder.WithBufferSize(bufferSize);
                                // для гарантии порядка событий
                                consumerBuilder
                                    .WithWorkDistributionStrategy<BytesSumDistributionStrategy>();
                                var consumerConfig = new ConsumerConfig
                                {
                                    AutoOffsetReset = AutoOffsetReset.Latest,
                                    ClientId = name,
                                    GroupInstanceId = name,
                                    PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
                                };
                                consumerBuilder.WithConsumerConfig(consumerConfig);
                                consumerBuilder.AddMiddlewares(
                                    middlewares =>
                                    {
                                        middlewares
                                            .AddSerializer<JsonCoreSerializer>();
                                        middlewares.AddTypedHandlers(handlers =>
                                            handlers.AddHandlers(groupConsumers.Select(r =>
                                                    executorType.MakeGenericType(r.EventType, r.ExecutorType)))
                                                .WithHandlerLifetime(InstanceLifetime.Scoped));
                                    }
                                );
                            }
                        );
                    }
                });
        });
    }
}

public class
    KafkaModuleOptionsValidator<TBaseTask, TDbContext> : TasksModuleOptionsValidator<TBaseTask, TDbContext,
        KafkaTasksModuleOptions<TBaseTask, TDbContext>> where TBaseTask : BaseTask
    where TDbContext : TasksDbContext<TBaseTask>
{
    public KafkaModuleOptionsValidator()
    {
        RuleFor(options => options.Brokers).NotEmpty().WithMessage("Specify Kafka brokers");
        RuleFor(options => options.TasksTopic).NotEmpty().WithMessage("Specify Kafka topic");
    }
}
