using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Data.Repository;

public class BaseTaskRepository<TTask, TBaseTask, TDbContext> : EFRepository<TTask, Guid, TDbContext>,
    ITaskRepository<TTask>
    where TTask : class, IBaseTask where TDbContext : TasksDbContext<TBaseTask> where TBaseTask : BaseTask
{
    public BaseTaskRepository(EFRepositoryContext<TTask, Guid, TDbContext> repositoryContext) : base(repositoryContext)
    {
    }
}

public class TasksRepository<TBaseTask, TDbContext> : EFRepository<TBaseTask, Guid, TDbContext>,
    ITaskRepository<TBaseTask>
    where TDbContext : TasksDbContext<TBaseTask> where TBaseTask : BaseTask
{
    public TasksRepository(EFRepositoryContext<TBaseTask, Guid, TDbContext> repositoryContext) : base(repositoryContext)
    {
    }
}

