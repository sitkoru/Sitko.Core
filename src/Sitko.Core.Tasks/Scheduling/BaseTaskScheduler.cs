using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace Sitko.Core.Tasks.Scheduling;

public abstract class BaseTaskScheduler<TTask> : BackgroundService where TTask : IBaseTask
{
    private readonly IOptions<TaskSchedulingOptions<TTask>> taskOptions;
    private readonly IOptionsMonitor<TasksModuleOptions> optionsMonitor;
    private readonly ITaskScheduler taskScheduler;
    protected ILogger<BaseTaskScheduler<TTask>> Logger { get; }

    protected BaseTaskScheduler(IOptions<TaskSchedulingOptions<TTask>> taskOptions,
        IOptionsMonitor<TasksModuleOptions> optionsMonitor,
        ITaskScheduler taskScheduler,
        ILogger<BaseTaskScheduler<TTask>> logger)
    {
        this.taskOptions = taskOptions;
        this.optionsMonitor = optionsMonitor;
        this.taskScheduler = taskScheduler;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ScheduleAsync(stoppingToken);
            try
            {
                await Task.Delay(taskOptions.Value.Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // do nothing
            }
        }
    }

    private async Task ScheduleAsync(CancellationToken cancellationToken)
    {
        if (optionsMonitor.CurrentValue.IsAllTasksDisabled ||
            optionsMonitor.CurrentValue.DisabledTasks.Contains(typeof(TTask).Name))
        {
            return;
        }

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var scheduleLock = ScheduleLocks.Locks.GetOrAdd(GetType().Name, _ => new AsyncLock());
        using (await scheduleLock.LockAsync(cts.Token))
        {
            var tasks = await GetTasksAsync(cancellationToken);
            foreach (var task in tasks)
            {
                await taskScheduler.ScheduleAsync(task);
            }
        }
    }

    protected abstract Task<TTask[]> GetTasksAsync(CancellationToken cancellationToken);
}
