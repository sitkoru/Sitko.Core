namespace Sitko.Core.Queue.Kafka;

[AttributeUsage(AttributeTargets.Class)]
public class QueueEventAttribute(string topic, string typeId) : Attribute
{
    public string Topic { get; } = topic;
    public string EventTypeId { get; } = typeId;
}
