using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Sitko.Core.Tasks.Components;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Scheduling;

public class TaskSchedulingService<TTask, TOptions> : BackgroundService where TTask : class, IBaseTask  where TOptions : TasksModuleOptions
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IOptions<TaskSchedulingOptions<TTask>> taskOptions;
    private readonly IOptionsMonitor<TOptions> optionsMonitor;
    private readonly ILogger<TaskSchedulingService<TTask, TOptions>> logger;

    public TaskSchedulingService(IServiceScopeFactory serviceScopeFactory,
        IOptions<TaskSchedulingOptions<TTask>> taskOptions, IOptionsMonitor<TOptions> optionsMonitor,
        ILogger<TaskSchedulingService<TTask, TOptions>> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.taskOptions = taskOptions;
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var nextDate = taskOptions.Value.CronExpression.GetNextOccurrence(now);
                if (nextDate != null)
                {
                    await Task.Delay(TimeSpan.FromSeconds((nextDate - now).Value.TotalSeconds), stoppingToken);
                }

                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IBaseTaskFactory<TTask>>();
                if (optionsMonitor.CurrentValue.IsAllTasksDisabled ||
                    optionsMonitor.CurrentValue.DisabledTasks.Contains(typeof(TTask).Name))
                {
                    return;
                }

                var tasksManager = scope.ServiceProvider.GetRequiredService<TasksManager>();
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                var scheduleLock = ScheduleLocks.Locks.GetOrAdd(GetType().Name, _ => new AsyncLock());
                using (await scheduleLock.LockAsync(cts.Token))
                {
                    var tasks = await scheduler.GetTasksAsync(stoppingToken);
                    foreach (var task in tasks)
                    {
                        try
                        {
                            var runResult = await tasksManager.RunAsync(task, cancellationToken: cts.Token);
                            if (!runResult.IsSuccess)
                            {
                                throw new Exception(runResult.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError("Error running task {Type}: {Ex}", typeof(TTask), ex);
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // do nothing
            }
            catch (Exception ex)
            {
                logger.LogError("Error schedule tasks {Type}: {Error}", typeof(TTask), ex);
            }
        }
    }
}
