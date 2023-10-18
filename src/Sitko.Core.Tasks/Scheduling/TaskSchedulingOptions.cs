using Cronos;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Scheduling;

public class TaskSchedulingOptions<TTask> where TTask : IBaseTask
{
    public CronExpression CronExpression { get; set; } = null!;
}
