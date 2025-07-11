using KafkaFlow.Producers;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Queue.Kafka;

public interface IEventProducer
{
    public void Produce<T>(string key, T message);
}

internal class EventProducer(
    KafkaMetadata metadata,
    IProducerAccessor producerAccessor,
    IOptions<KafkaQueueModuleOptions> options) : IEventProducer
{
    public void Produce<T>(string key, T message)
    {
        var messageMetadata = metadata.GetByType<T>();
        producerAccessor.GetProducer(options.Value.ProducerName)
            .Produce(messageMetadata.PrefixedTopicName, key, message);
    }
}
