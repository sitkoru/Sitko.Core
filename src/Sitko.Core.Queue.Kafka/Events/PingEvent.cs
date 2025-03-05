namespace Sitko.Core.Queue.Kafka.Events;

public class PingEvent : IBaseEvent
{
    public string GetKey() => Guid.NewGuid().ToString();
}
