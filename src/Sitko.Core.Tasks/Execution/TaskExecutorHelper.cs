using Sitko.Core.Queue.Kafka.Attributes;

namespace Sitko.Core.Tasks.Execution;

internal static class TaskExecutorHelper
{
    public static (string GroupId, int ParallelThreadCount, int BufferSize, bool allowRetry)? GetGroupInfo(Type eventProcessorType)
    {
        var attribute = eventProcessorType.FindAttribute<TaskExecutorAttribute>();
        if (attribute is null)
        {
            return null;
        }

        var groupId = attribute.GroupId;
        return (groupId, attribute.ParallelThreadCount, attribute.BufferSize, attribute.AllowRetry);
    }
}
