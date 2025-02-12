namespace Sitko.Core.Queue.Kafka.Events;

public abstract class BaseEvent
{
    public abstract string ObjectId { get; set; }
    public virtual DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
}
