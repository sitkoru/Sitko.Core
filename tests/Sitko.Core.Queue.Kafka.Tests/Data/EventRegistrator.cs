using System.Collections.Concurrent;

namespace Sitko.Core.Queue.Kafka.Tests.Data;

public static class EventRegistrator
{
    private static readonly ConcurrentBag<Guid> RegisteredEvents = new();
    private static readonly ConcurrentDictionary<int, Guid[]> RegisteredBatches = new();
    public static int BatchesCount => RegisteredBatches.Count;
    public static int BatchesMessagesCount => RegisteredBatches.SelectMany(x => x.Value).Count();
    public static int ProcessedCount => RegisteredEvents.Distinct().Count();

    public static void Register(Guid messageId) => RegisteredEvents.Add(messageId);
    public static void RegisterBatch(Guid[] messageIds) => RegisteredBatches[RegisteredBatches.Count] = messageIds;

    public static bool IsRegistered(Guid messageId) => RegisteredEvents.Contains(messageId);
}
