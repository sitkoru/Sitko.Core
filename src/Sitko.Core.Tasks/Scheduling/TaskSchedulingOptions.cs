namespace Sitko.Core.Tasks.Scheduling;

public class TaskSchedulingOptions<TTask> where TTask : IBaseTask
{
    public TimeSpan Interval { get; set; }
}
