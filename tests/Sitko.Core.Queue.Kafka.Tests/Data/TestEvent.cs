using Sitko.Core.Queue.Kafka.Events;

namespace Sitko.Core.Queue.Kafka.Tests.Data;

public class TestEvent : IBaseEvent
{
    public string ObjectId { get; set; }
    public string GetKey() => Guid.NewGuid().ToString();
}

public static class TestEventData
{
    public static TestEvent Message { get; set; } = new()
    {
        ObjectId = Guid.NewGuid().ToString()
    };
}
