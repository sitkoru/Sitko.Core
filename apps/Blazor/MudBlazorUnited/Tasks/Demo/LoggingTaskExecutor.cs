using Sitko.Core.Tasks.Execution;

namespace MudBlazorUnited.Tasks.Demo;

[TaskExecutor("Loggers", 10)]
public class LoggingTaskExecutor : BaseTaskExecutor<LoggingTask, LoggingTaskConfig, LoggingTaskResult>
{
    public LoggingTaskExecutor(ITaskExecutorContext<LoggingTask> executorContext, ILogger<LoggingTaskExecutor> logger) :
        base(executorContext, logger)
    {
    }

    protected override Task<LoggingTaskResult> ExecuteAsync(LoggingTask task, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Run Logging Task: {Id}", task.Config.Id);
        return Task.FromResult(new LoggingTaskResult());
    }
}
