using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App.Results;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Data.Repository;
using Sitko.Core.Tasks.Scheduling;

namespace Sitko.Core.Tasks.Components;

public class TasksManager
{
    private readonly IEnumerable<IRepository> repositories;
    private readonly IServiceProvider serviceProvider;

    public TasksManager(IEnumerable<IRepository> repositories, IServiceProvider serviceProvider)
    {
        this.repositories = repositories;
        this.serviceProvider = serviceProvider;
    }

    private ITaskRepository<TTask> GetRepository<TTask>() where TTask : class, IBaseTask
    {
        foreach (var repository in repositories)
        {
            if (repository is ITaskRepository<TTask> taskRepository)
            {
                return taskRepository;
            }
        }

        throw new InvalidOperationException("Repository not found");
    }

    public Task<TTask?> GetByIdAsync<TTask>(Guid id, CancellationToken cancellationToken = default)
        where TTask : class, IBaseTask => GetRepository<TTask>().GetByIdAsync(id, cancellationToken);

    public async Task<TTask?> GetParentAsync<TTask>(Guid id, CancellationToken cancellationToken = default)
        where TTask : class, IBaseTask
    {
        var task = await GetByIdAsync<TTask>(id, cancellationToken);
        if (task is { ParentId: { } })
        {
            return await GetByIdAsync<TTask>(task.ParentId.Value, cancellationToken);
        }

        return null;
    }

    public async Task<TTask[]?> GetChildrenAsync<TTask>(Guid id, CancellationToken cancellationToken = default)
        where TTask : class, IBaseTask
    {
        var (childTasks, _) =
            await GetRepository<TTask>().GetAllAsync(q => q.Where(t => t.ParentId == id), cancellationToken);
        return childTasks;
    }

    public async Task<OperationResult<TTask>> RunAsync<TTask>(TTask newTask, Guid? parentId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
        where TTask : class, IBaseTask
    {
        var repository = GetRepository<TTask>();
        newTask.ParentId = parentId;
        newTask.UserId = userId;
        var addResponse = await repository.AddAsync(newTask, cancellationToken);
        if (!addResponse.IsSuccess)
        {
            return new OperationResult<TTask>(addResponse.ErrorsString);
        }

        var taskScheduler = serviceProvider.GetRequiredService<ITaskScheduler<TTask>>();
        await taskScheduler.ScheduleAsync(newTask);

        return new OperationResult<TTask>(addResponse.Entity);
    }
}
