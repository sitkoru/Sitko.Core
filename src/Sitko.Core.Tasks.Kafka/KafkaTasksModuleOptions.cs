using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Kafka;

public class KafkaTasksModuleOptions<TBaseTask, TDbContext> : TasksModuleOptions<TBaseTask, TDbContext>
    where TBaseTask : BaseTask
    where TDbContext : TasksDbContext<TBaseTask>
{
    public override Type GetValidatorType() => typeof(KafkaModuleOptionsValidator<TBaseTask, TDbContext>);
    public string TasksTopic { get; set; } = "";
    public bool AddTopicPrefix { get; set; } = true;
    public string TopicPrefix { get; set; } = "";
    public int TopicPartitions { get; set; } = 24;
    public short TopicReplicationFactor { get; set; } = 1;
    public bool AddConsumerGroupPrefix { get; set; } = true;
    public string ConsumerGroupPrefix { get; set; } = "";
}
