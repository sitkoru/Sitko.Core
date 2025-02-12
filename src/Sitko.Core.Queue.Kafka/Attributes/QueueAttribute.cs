namespace Sitko.Core.Queue.Kafka.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class QueueAttribute(string topic) : Attribute
{
    public string Topic { get; } = topic;
}
