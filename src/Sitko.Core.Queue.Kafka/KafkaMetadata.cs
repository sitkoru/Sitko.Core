namespace Sitko.Core.Queue.Kafka;

internal class KafkaMetadata(Dictionary<Type, EventMetadata> events)
{
    private Dictionary<Type, EventMetadata> Events { get; } = events;

    public EventMetadata GetByType(Type type) => Events.TryGetValue(type, out var metadata)
        ? metadata
        : throw new InvalidOperationException($"Message with type {type} not registered");

    public EventMetadata GetByType<T>() => GetByType(typeof(T));

    public EventMetadata GetByTypeId(string typeId) =>
        Events.Values.FirstOrDefault(x => x.EventTypeId.Equals(typeId, StringComparison.OrdinalIgnoreCase)) ??
        throw new InvalidOperationException("Can't find message type with id " + typeId);
}
