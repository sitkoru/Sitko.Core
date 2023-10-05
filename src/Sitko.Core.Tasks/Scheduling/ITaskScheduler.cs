using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Scheduling;

public interface ITaskScheduler<TTask> where TTask : IBaseTask
{
    Task ScheduleAsync(TTask task);
}
