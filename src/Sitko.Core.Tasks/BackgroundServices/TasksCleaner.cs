using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Data.Repository;

namespace Sitko.Core.Tasks.BackgroundServices;

public class TasksCleaner<TBaseTask, TOptions> : BaseService
    where TBaseTask : BaseTask
    where TOptions : TasksModuleOptions
{
    private readonly IOptions<TOptions> options;
    private readonly ILogger<TasksCleaner<TBaseTask, TOptions>> logger;
    protected override TimeSpan InitDelay => TimeSpan.FromMinutes(2);
    protected override TimeSpan RunDelay => TimeSpan.FromDays(1);

    public TasksCleaner(IServiceScopeFactory serviceScopeFactory, ILogger<BaseService> logger,
        IOptions<TOptions> options, ILogger<TasksCleaner<TBaseTask, TOptions>> logger1) : base(serviceScopeFactory,
        logger)
    {
        this.options = options;
        this.logger = logger1;
    }

    protected override async Task ExecuteAsync(AsyncServiceScope scope, CancellationToken stoppingToken)
    {
        var tasksRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository<TBaseTask>>();
        if (options.Value.AllTasksRetentionDays is > 0)
        {
            var taskTypes = options.Value.RetentionDays.Select(r => r.Key).ToArray();
            await RemoveTasksAsync(tasksRepository, options.Value.AllTasksRetentionDays.Value, false, taskTypes,
                stoppingToken);
        }

        foreach (var (taskType, retentionDays) in options.Value.RetentionDays)
        {
            if (retentionDays > 0)
            {
                await RemoveTasksAsync(tasksRepository, retentionDays, true, new[] { taskType }, stoppingToken);
            }
        }
    }

    private async Task RemoveTasksAsync(ITaskRepository<TBaseTask> tasksRepository, int retentionDays, bool include,
        string[] types, CancellationToken stoppingToken)
    {
        var date = DateTimeOffset.UtcNow.AddDays(retentionDays * -1);
        logger.LogInformation("Deleting tasks from {Date}. Types: {Types}, include: {Include}", date, types, include);
        if (tasksRepository is IEFRepository<TBaseTask> efRepository)
        {
            var deletedCount = await efRepository.DeleteAllAsync(task => task.DateAdded < date && (types.Length == 0 ||
                (include
                    ? types.Contains(task.Type)
                    : !types.Contains(task.Type))), stoppingToken);
            logger.LogInformation("Deleted {Count} tasks", deletedCount);
        }
        else
        {
            var (tasks, tasksCount) =
                await tasksRepository.GetAllAsync(query => query.Where(task => task.DateAdded < date &&
                                                                               (types.Length == 0 ||
                                                                                   (include
                                                                                       ? types.Contains(task.Type)
                                                                                       : !types.Contains(task.Type)))),
                    stoppingToken);
            if (tasksCount > 0)
            {
                foreach (var task in tasks)
                {
                    await tasksRepository.DeleteAsync(task, stoppingToken);
                }

                logger.LogInformation("Deleted {Count} tasks", tasksCount);
            }
        }
    }
}
