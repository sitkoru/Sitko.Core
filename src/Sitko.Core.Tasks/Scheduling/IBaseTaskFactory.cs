using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Scheduling;

public interface IBaseTaskFactory<TTask> where TTask : IBaseTask
{
    public Task<TTask[]> GetTasksAsync(CancellationToken cancellationToken);
}
