using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Repository.EntityFrameworkCore;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Data.Repository;

namespace Sitko.Core.Tasks.BackgroundServices;

public class TasksCleaner<TBaseTask> : BackgroundService
    where TBaseTask : BaseTask
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IOptions<TasksModuleOptions> options;
    private readonly ILogger<TasksCleaner<TBaseTask>> logger;
    private ITaskRepository<TBaseTask> tasksRepository;

    public TasksCleaner(IOptions<TasksModuleOptions> options, ILogger<TasksCleaner<TBaseTask>> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.options = options;
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            tasksRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository<TBaseTask>>();
            if (options.Value.AllTasksRetentionDays is > 0)
            {
                var taskTypes = options.Value.RetentionDays.Select(r => r.Key).ToArray();
                await RemoveTasks(options.Value.AllTasksRetentionDays.Value, false, taskTypes);
            }

            foreach (var (taskType, retentionDays) in options.Value.RetentionDays)
            {
                if (retentionDays > 0)
                {
                    await RemoveTasks(retentionDays, true, new[] { taskType });
                }
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }

    private async Task RemoveTasks(int retentionDays, bool include, string[] types)
    {
        var date = DateTimeOffset.UtcNow.AddDays(retentionDays * -1);
        logger.LogInformation("Deleting tasks from {Date}. Types: {Types}, include: {Include}", date, types, include);
        if (tasksRepository is IEFRepository efRepository)
        {
            var condition = $"\"{nameof(BaseTask.DateAdded)}\" < '{date.ToString("O", CultureInfo.InvariantCulture)}'";
            if (types.Length > 0)
            {
                condition +=
                    $" AND \"{nameof(BaseTask.Type)}\" {(include ? "IN" : "NOT IN")} ({string.Join(",", types.Select(type => $"'{type}'"))})";
            }

            var deletedCount = await efRepository.DeleteAllRawAsync(condition);
            logger.LogInformation("Deleted {Count} tasks", deletedCount);
        }
        else
        {
            var (tasks, tasksCount) =
                await tasksRepository.GetAllAsync(query => query.Where(task => task.DateAdded < date &&
                                                                               (types.Length == 0 ||
                                                                                   (include
                                                                                       ? types.Contains(task.Type)
                                                                                       : !types.Contains(task.Type)))));
            if (tasksCount > 0)
            {
                foreach (var task in tasks)
                {
                    await tasksRepository.DeleteAsync(task);
                }

                logger.LogInformation("Deleted {Count} tasks", tasksCount);
            }
        }
    }
}
