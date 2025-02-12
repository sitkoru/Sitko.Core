using Sitko.Core.Queue.Kafka.Attributes;

namespace Sitko.Core.Tasks.Execution;

[AttributeUsage(AttributeTargets.Class)]
public class TaskExecutorAttribute
    (string groupId, int parallelThreadCount, int bufferSize = 10, bool allowRetry = false)
    : MessageHandlerAttribute(groupId, parallelThreadCount, bufferSize)
{
    public bool AllowRetry { get; } = allowRetry;
}
