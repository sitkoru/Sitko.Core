using Confluent.Kafka;
using KafkaFlow;
using KafkaFlow.Producers;
using Sitko.Core.Queue.Kafka.Events;

namespace Sitko.Core.Queue.Kafka.Producing;

internal class EventProducer(IProducerAccessor producerAccessor) : IEventProducer
{
    private const int MaxProducingBatchSize = 100;

    public async Task<EventProducingResult> ProduceAsync<TEvent>(TEvent @event) where TEvent : IBaseEvent
    {
        var (topic, producerName) = EventsRegistry.GetProducerName(@event.GetType());
        var producer = producerAccessor.GetProducer(producerName);
        return await ProduceAsync(producer, topic, @event).ConfigureAwait(false);
    }

    public async Task ProduceAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : IBaseEvent
    {
        foreach (var eventBatch in events.Chunk(MaxProducingBatchSize))
        {
            await Parallel.ForEachAsync(eventBatch, async (@event, _) =>
            {
                await ProduceAsync(@event);
            });
        }
    }

    public async Task PingAsync()
    {
        var pingEvent = new PingEvent();
        var (_, topic) = EventsRegistry.GetProducerName(pingEvent.GetType());

        foreach (var producer in producerAccessor.All)
        {
            await ProduceAsync(producer, topic, pingEvent).ConfigureAwait(false);
        }
    }

    private static async Task<EventProducingResult> ProduceAsync<TEvent>
        (IMessageProducer? producer, string topic, TEvent @event)
        where TEvent : IBaseEvent
    {
        if (producer == null)
        {
            return new EventProducingResult("Producer is null or empty");
        }

        var result = await producer.ProduceAsync(topic, @event.GetKey(), @event).ConfigureAwait(false);
        if (result.Status != PersistenceStatus.Persisted)
        {
            throw new InvalidOperationException("Message was not persisted");
        }

        return new(result.Partition.Value, result.Offset.Value);
    }
}
