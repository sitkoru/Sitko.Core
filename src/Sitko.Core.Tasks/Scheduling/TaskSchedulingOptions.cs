using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Scheduling;

public class TaskSchedulingOptions<TTask> where TTask : IBaseTask
{
    public string Interval { get; set; } = "";
}
