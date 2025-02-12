using Sitko.Core.Queue.Kafka.Events;

namespace Sitko.Core.Queue.Kafka.Tests.Data;

public class TestEvent : BaseEvent
{
    public override string ObjectId { get; set; }
}

public static class TestEventData
{
    public static TestEvent Message { get; set; } = new()
    {
        ObjectId = Guid.NewGuid().ToString()
    };
}
