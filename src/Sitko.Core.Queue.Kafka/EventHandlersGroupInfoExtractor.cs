using Sitko.Core.Queue.Kafka.Attributes;

namespace Sitko.Core.Queue.Kafka;

public class EventHandlersGroupInfoExtractor
{
    private static string prefix = string.Empty;

    public static void SetPrefix(string? newPrefix) => prefix = newPrefix ?? string.Empty;

    internal static (string GroupId, string GroupIdWithPrefix, int ParallelThreadCount, int BufferSize)? GetGroupInfo(Type eventProcessorType)
    {
        var attribute = eventProcessorType.FindAttribute<MessageHandlerAttribute>();
        if (attribute is null)
        {
            return null;
        }

        var groupId = attribute.GroupId;
        return (groupId, GetGroupIdWithPrefix(groupId), attribute.ParallelThreadCount, attribute.BufferSize);
    }

    internal static string GetGroupIdWithPrefix(string groupId) =>
        string.IsNullOrEmpty(prefix) || groupId.StartsWith(prefix) ? groupId : $"{prefix}{groupId}";
}
