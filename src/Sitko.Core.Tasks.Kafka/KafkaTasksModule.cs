using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;
using Sitko.Core.Queue.Kafka;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Execution;
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

        var kafkaConfigurator = KafkaModule.CreateConfigurator("Kafka_Tasks_Cluster");
        var producerName = $"Tasks_{typeof(TBaseTask).Name}";

        foreach (var executor in executors)
        {
            kafkaConfigurator.RegisterEvent(executor.EventType, kafkaTopic, producerName);
            kafkaConfigurator.AddConsumer(executor.ExecutorType, applicationContext,
            [
                new TopicInfo(kafkaTopic, startupOptions.TopicPartitions, startupOptions.TopicReplicationFactor)
            ], kafkaGroupPrefix);
        }

        kafkaConfigurator
            .AutoCreateTopic(kafkaTopic, startupOptions.TopicPartitions, startupOptions.TopicReplicationFactor)
            .EnsureOffsets()
            .AddProducer(producerName, kafkaTopic);
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
