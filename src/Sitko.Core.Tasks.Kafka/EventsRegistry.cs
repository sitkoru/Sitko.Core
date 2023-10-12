namespace Sitko.Core.Tasks.Kafka;

internal static class EventsRegistry
{
    private static readonly Dictionary<Type, (string Topic, string ProducerName)> events = new();

    public static void Register(Type eventType, string topic, string producerName) => events[eventType] = (topic, producerName);

    public static string GetProducerName(Type eventType) => events.TryGetValue(eventType, out var eventData)
        ? eventData.ProducerName
        : throw new InvalidOperationException($"Can't find producer for event {eventType}");
}
