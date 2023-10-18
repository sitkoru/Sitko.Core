using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Scheduling;

public interface ITaskScheduler
{
    Task ScheduleAsync(IBaseTask task);
}
