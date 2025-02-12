namespace Sitko.Core.Queue.Kafka.Events;

internal static class EventsRegistry
{
    private static readonly Dictionary<Type, (string Topic, string ProducerName)> events = new();

    public static void Register(Type eventType, string topic, string producerName) => events[eventType] = (topic, producerName);

    public static (string Topic, string ProducerName) GetProducerName(Type eventType) => events.TryGetValue(eventType, out var eventData)
        ? (eventData.Topic, eventData.ProducerName)
        : throw new InvalidOperationException($"Can't find producer for event {eventType}");
}

public static class EventsProvider
{
    public static string GetProducerName(Type eventType) => EventsRegistry.GetProducerName(eventType).ProducerName;
}
