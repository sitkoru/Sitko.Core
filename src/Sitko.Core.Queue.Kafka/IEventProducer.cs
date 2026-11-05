using Confluent.Kafka;
using KafkaFlow.Producers;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;

namespace Sitko.Core.Queue.Kafka;

public interface IEventProducer
{
    public void Produce<T>(string key, T message);

    /// <summary>Waits for a confirmed broker write and returns its delivery report.</summary>
    /// <remarks>
    /// Retries are opt-in and limited to safe local queue-full failures. Stopping the wait does not cancel sending.
    /// Exceptions without a reliable delivery report do not prove that the message was not written.
    /// </remarks>
    public Task<DeliveryResult<byte[], byte[]>> ProduceAsync<T>(string key, T message);
}

internal class EventProducer(
    KafkaMetadata metadata,
    IProducerAccessor producerAccessor,
    IOptions<KafkaQueueModuleOptions> options,
    IEnumerable<KafkaProducerResilienceRegistration> resilienceRegistrations,
    ResiliencePipelineProvider<KafkaProducerResilienceRegistration>? pipelineProvider = null) : IEventProducer
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
        var producer = producerAccessor.GetProducer(options.Value.ProducerName);
        var registration = resilienceRegistrations.LastOrDefault(item => item.EventType == typeof(T)) ??
                           resilienceRegistrations.LastOrDefault(item => item.EventType is null);
        DeliveryResult<byte[], byte[]> report;
        if (registration is { Enabled: true })
        {
            var pipeline = pipelineProvider!.GetPipeline(registration);
            var context = ResilienceContextPool.Shared.Get(messageMetadata.PrefixedTopicName);
            try
            {
                report = await pipeline.ExecuteAsync(async _ =>
                    await producer.ProduceAsync(messageMetadata.PrefixedTopicName, key, message), context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
        }
        else
        {
            report = await producer.ProduceAsync(messageMetadata.PrefixedTopicName, key, message);
        }
        if (report is null || report.Status != PersistenceStatus.Persisted ||
            string.IsNullOrWhiteSpace(report.Topic) || report.Partition.Value < 0 || report.Offset.Value < 0)
        {
            throw new InvalidOperationException("Kafka did not return a confirmed delivery report. Delivery outcome is unknown.");
        }

        return report;
    }
}
