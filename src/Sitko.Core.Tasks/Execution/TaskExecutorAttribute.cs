namespace Sitko.Core.Tasks.Execution;

[AttributeUsage(AttributeTargets.Class)]
public class TaskExecutorAttribute : Attribute
{
    public TaskExecutorAttribute(string groupId, int parallelThreadCount, int bufferSize = 10, bool allowRetry = false)
    {
        GroupId = groupId;
        ParallelThreadCount = Math.Max(parallelThreadCount, 1);
        BufferSize = Math.Max(bufferSize, 1);
        AllowRetry = allowRetry;
    }

    public string GroupId { get; }

    public int ParallelThreadCount { get; }

    public int BufferSize { get; }

    public bool AllowRetry { get; }
}
