namespace Sitko.Core.Queue.Kafka;

[AttributeUsage(AttributeTargets.Class)]
public class QueueBatchHandlerAttribute(
    string group,
    int batchSize = 100,
    int batchTimeoutInSeconds = 10,
    int parallelThreadCount = 20,
    int bufferSize = 10) : Attribute
{
    public string Group { get; } = group;
    public int BatchSize { get; } = batchSize;
    public int BatchTimeoutInSeconds { get; } = batchTimeoutInSeconds;
    public int ParallelThreadCount { get; } = parallelThreadCount;
    public int BufferSize { get; } = bufferSize;
}
