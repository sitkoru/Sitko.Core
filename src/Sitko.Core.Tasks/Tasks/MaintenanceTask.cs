using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sitko.Core.Tasks.Data;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Data.Repository;
using Sitko.Core.Tasks.Execution;
using TaskStatus = Sitko.Core.Tasks.Data.Entities.TaskStatus;

namespace Sitko.Core.Tasks.Tasks;

public class MaintenanceTask<TBaseTask, TDbContext> : BackgroundService
    where TBaseTask : BaseTask where TDbContext : TasksDbContext<TBaseTask>
{
    private readonly IOptions<TasksModuleOptions> options;
    private readonly IServiceScopeFactory serviceScopeFactory;

    public MaintenanceTask(IOptions<TasksModuleOptions> options,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.options = options;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var tasksRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository<TBaseTask>>();
            var inactivityDate = DateTimeOffset.UtcNow - options.Value.TasksInactivityTimeout;
            var waitDate = DateTimeOffset.UtcNow - options.Value.TasksWaitTimeout;
            var stuckTasks = await tasksRepository.GetAllAsync(query =>
                query.Where(task =>
                    (task.TaskStatus == TaskStatus.InProgress && task.LastActivityDate < inactivityDate) ||
                    (task.TaskStatus == TaskStatus.Wait && task.DateAdded < waitDate)), stoppingToken);
            if (stuckTasks.items.Length > 0)
            {
                switch (options.Value.StuckTasksProcessMode)
                {
                    case StuckTasksProcessMode.Fail:
                        await tasksRepository.BeginBatchAsync(stoppingToken);
                        foreach (var task in stuckTasks.items)
                        {
                            var error = task.TaskStatus == TaskStatus.InProgress
                                ? $"Task inactive since {task.LastActivityDate}"
                                : $"Task in queue since {task.DateAdded}";
                            TasksExtensions.SetTaskErrorResult((IBaseTaskWithConfigAndResult)task, error);
                            await tasksRepository.UpdateAsync(task, stoppingToken);
                        }

                        await tasksRepository.CommitBatchAsync(stoppingToken);
                        break;
                    case StuckTasksProcessMode.Restart:
                        var taskExecutor = scope.ServiceProvider.GetRequiredService<ITaskExecutor>();
                        foreach (var stuckTask in stuckTasks.items)
                        {
                            await taskExecutor.ExecuteAsync(stuckTask.Id, stoppingToken);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
