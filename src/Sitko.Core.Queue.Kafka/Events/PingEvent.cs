using Sitko.Core.Queue.Kafka.Attributes;

namespace Sitko.Core.Queue.Kafka.Events;

[Queue("Ping")]
public class PingEvent : BaseEvent
{
    public override string ObjectId { get; set; } = Guid.NewGuid().ToString();
}
