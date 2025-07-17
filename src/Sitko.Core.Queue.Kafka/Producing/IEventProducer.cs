using Sitko.Core.Queue.Kafka.Events;

namespace Sitko.Core.Queue.Kafka.Producing;

public interface IEventProducer
{
    Task<EventProducingResult> ProduceAsync<TEvent>(TEvent @event) where TEvent : IBaseEvent;

    Task ProduceAsync<TEvent>(IEnumerable<TEvent> events) where TEvent : IBaseEvent;

    Task PingAsync();
}

public record EventProducingResult
{
    public EventProducingResult(int partition, long offset)
    {
        Partition = partition;
        Offset = offset;
    }

    public EventProducingResult(string errorMessage) =>
        ErrorMessage = errorMessage;

    public EventProducingResult(Exception exception, string? errorMessage = null)
    {
        Exception = exception;
        ErrorMessage = errorMessage;
    }

    public int Partition { get; set; }
    public long Offset { get; set; }
    public string? ErrorMessage { get; }
    public Exception? Exception { get; }
}
