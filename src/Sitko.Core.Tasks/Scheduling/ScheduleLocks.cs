using System.Collections.Concurrent;
using Nito.AsyncEx;

namespace Sitko.Core.Tasks.Scheduling;

public static class ScheduleLocks
{
    public static ConcurrentDictionary<string, AsyncLock> Locks { get; } = new();
}
