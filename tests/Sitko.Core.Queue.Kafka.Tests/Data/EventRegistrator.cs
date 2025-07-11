namespace Sitko.Core.Queue.Kafka.Tests.Data;

public static class EventRegistrator
{
    private static readonly HashSet<Guid> RegisteredEvents = new();

    public static void Register(Guid messageId) => RegisteredEvents.Add(messageId);

    public static bool IsRegistered(Guid messageId) => RegisteredEvents.Contains(messageId);
}
