namespace Sitko.Core.Queue.Kafka.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class MessageHandlerAttribute(
    string groupId,
    int parallelThreadCount,
    int bufferSize = 10)
    : Attribute
{
    public string GroupId { get; } = groupId;

    public int ParallelThreadCount { get; } = Math.Max(parallelThreadCount, 1);

    public int BufferSize { get; } = Math.Max(bufferSize, 1);
}
