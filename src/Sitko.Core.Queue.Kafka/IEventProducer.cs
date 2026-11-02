using Confluent.Kafka;
using KafkaFlow.Producers;
using Microsoft.Extensions.Options;

namespace Sitko.Core.Queue.Kafka;

public interface IEventProducer
{
    public void Produce<T>(string key, T message);

    /// <summary>Waits for a confirmed broker write and returns its delivery report.</summary>
    /// <remarks>
    /// Does not add retries or cancel sending when the caller stops waiting.
    /// Exceptions without a reliable delivery report do not prove that the message was not written.
    /// </remarks>
    public Task<DeliveryResult<byte[], byte[]>> ProduceAsync<T>(string key, T message);
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

    public async Task<DeliveryResult<byte[], byte[]>> ProduceAsync<T>(string key, T message)
    {
        var messageMetadata = metadata.GetByType<T>();
        var report = await producerAccessor.GetProducer(options.Value.ProducerName)
            .ProduceAsync(messageMetadata.PrefixedTopicName, key, message);
        if (report is null || report.Status != PersistenceStatus.Persisted ||
            string.IsNullOrWhiteSpace(report.Topic) || report.Partition.Value < 0 || report.Offset.Value < 0)
        {
            throw new InvalidOperationException("Kafka did not return a confirmed delivery report. Delivery outcome is unknown.");
        }

        return report;
    }
}
