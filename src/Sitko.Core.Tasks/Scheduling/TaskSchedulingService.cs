using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.Tasks.Components;
using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Scheduling;

public class TaskSchedulingService<TTask, TOptions> : BackgroundService
    where TTask : class, IBaseTask where TOptions : TasksModuleOptions
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IOptions<TaskSchedulingOptions<TTask>> taskOptions;
    private readonly IOptionsMonitor<TOptions> optionsMonitor;
    private readonly ILogger<TaskSchedulingService<TTask, TOptions>> logger;
    private readonly IDistributedLock? distributedLock;

    public TaskSchedulingService(IServiceScopeFactory serviceScopeFactory,
        IOptions<TaskSchedulingOptions<TTask>> taskOptions, IOptionsMonitor<TOptions> optionsMonitor,
        ILogger<TaskSchedulingService<TTask, TOptions>> logger, IApplicationContext applicationContext,
        IDistributedLockProvider? distributedLockProvider = null)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.taskOptions = taskOptions;
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
        distributedLock = distributedLockProvider?.CreateLock
            ($"{applicationContext.Name}_{applicationContext.Environment}_{typeof(TTask).Name}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Init scheduling service {Type}", typeof(TTask));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (optionsMonitor.CurrentValue.UseDistributedLock && distributedLock != null)
                {
                    await using var handle = await distributedLock.TryAcquireAsync
                        (TimeSpan.FromSeconds(optionsMonitor.CurrentValue.AcquireTimeoutInSeconds), cancellationToken: stoppingToken);
                    if (handle is null)
                    {
                        logger.LogInformation("Skip scheduling task {Type}: lock is held by another instance", typeof(TTask));
                        await Task.Delay(TimeSpan.FromSeconds(optionsMonitor.CurrentValue.RetryDelayInSeconds), stoppingToken);
                        continue;
                    }
                }

                logger.LogInformation("Calculate next scheduling time for task {Type}", typeof(TTask));
                var now = DateTime.UtcNow;
                var nextDate = taskOptions.Value.CronExpression.GetNextOccurrence(now);
                logger.LogInformation("Next scheduling time for task {Type}: {Date}", typeof(TTask), nextDate);
                if (nextDate != null)
                {
                    var secondsToWait = Math.Round((nextDate - now).Value.TotalSeconds,
                        MidpointRounding.ToPositiveInfinity);
                    logger.LogInformation("Wait {Seconds} seconds before scheduling task {Type}", secondsToWait,
                        typeof(TTask));
                    await Task.Delay(TimeSpan.FromSeconds(secondsToWait), stoppingToken);
                }

                logger.LogInformation("Run scheduling task {Type}", typeof(TTask));
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IBaseTaskFactory<TTask>>();
                if (optionsMonitor.CurrentValue.IsAllTasksDisabled ||
                    optionsMonitor.CurrentValue.DisabledTasks.Contains(typeof(TTask).Name))
                {
                    logger.LogInformation("Skip disabled task {Type}", typeof(TTask));
                    return;
                }

                var tasksManager = scope.ServiceProvider.GetRequiredService<TasksManager>();
                var tasks = await scheduler.GetTasksAsync(stoppingToken);
                logger.LogInformation("Found {Count} {Type} tasks", tasks.Length, typeof(TTask));
                foreach (var task in tasks)
                {
                    try
                    {
                        var runResult = await tasksManager
                            .RunAsync(task, cancellationToken: stoppingToken) // cancels if application is stopping
                            .WaitAsync(TimeSpan.FromMinutes(1), stoppingToken); // don't hang too long
                        if (!runResult.IsSuccess)
                        {
                            throw new InvalidOperationException(runResult.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError("Error running task {Type}: {Ex}", typeof(TTask), ex);
                    }
                }

                logger.LogInformation("Scheduling task {Type} success", typeof(TTask));
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("Scheduling task {Type} is canceled", typeof(TTask));
                // do nothing
            }
            catch (Exception ex)
            {
                logger.LogError("Error schedule tasks {Type}: {Error}", typeof(TTask), ex);
            }
        }

        logger.LogInformation("Exit from scheduling task {Type}", typeof(TTask));
    }
}
