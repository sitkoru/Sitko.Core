namespace Sitko.Core.Tasks.Execution;

[AttributeUsage(AttributeTargets.Class)]
public class TaskExecutorAttribute : Attribute
{
    public TaskExecutorAttribute(string groupId, int parallelThreadCount, int bufferSize = 10)
    {
        GroupId = groupId;
        ParallelThreadCount = Math.Max(parallelThreadCount, 1);
        BufferSize = Math.Max(bufferSize, 1);
    }

    public string GroupId { get; }

    public int ParallelThreadCount { get; }

    public int BufferSize { get; }
}
