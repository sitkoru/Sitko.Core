using System.Reflection;

namespace Sitko.Core.Tasks.Execution;

internal static class TaskExecutorHelper
{
    public static (string GroupId, int ParallelThreadCount, int BufferSize)? GetGroupInfo(Type eventProcessorType)
    {
        var attribute = eventProcessorType.FindAttribute<TaskExecutorAttribute>();
        if (attribute is null)
        {
            return null;
        }

        var groupId = attribute.GroupId;
        return (groupId, attribute.ParallelThreadCount, attribute.BufferSize);
    }

    public static TResult? FindAttribute<TResult>(this ICustomAttributeProvider provider, bool withInherit = true)
        where TResult : Attribute =>
        provider.GetCustomAttributes(typeof(TResult), withInherit).Cast<TResult>().FirstOrDefault();
}
