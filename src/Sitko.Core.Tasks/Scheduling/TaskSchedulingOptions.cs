using Cronos;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Scheduling;

// ReSharper disable once UnusedTypeParameter
public class TaskSchedulingOptions<TTask> where TTask : IBaseTask
{
    public CronExpression CronExpression { get; set; } = null!;
}
