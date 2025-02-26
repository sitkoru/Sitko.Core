namespace Sitko.Core.Queue.Kafka.Events;

public class PingEvent : BaseEvent
{
    public override string ObjectId { get; set; } = Guid.NewGuid().ToString();
}
