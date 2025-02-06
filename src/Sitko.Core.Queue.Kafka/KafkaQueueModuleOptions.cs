using Sitko.Core.App;

namespace Sitko.Core.Queue.Kafka;

public class KafkaQueueModuleOptions : BaseApplicationModuleOptions
{
    public string QueueTopic { get; set; } = "";
    public bool AddTopicPrefix { get; set; } = true;
    public string TopicPrefix { get; set; } = "";
    public int TopicPartitions { get; set; } = 24;
    public short TopicReplicationFactor { get; set; } = 1;
    public bool AddConsumerGroupPrefix { get; set; } = true;
    public string ConsumerGroupPrefix { get; set; } = "";


    public List<Type> Messages { get; } = new();
    public List<Type> Consumers { get; } = new();

    public KafkaQueueModuleOptions AddMessage<TMessage>()
    {
        Messages.Add(typeof(TMessage));
        return this;
    }

    public KafkaQueueModuleOptions AddConsumer<TConsumer, TMessage>()
        where TConsumer : IQueueConsumer<TMessage>
    {
        Consumers.Add(typeof(TConsumer));
        return this;
    }
}
