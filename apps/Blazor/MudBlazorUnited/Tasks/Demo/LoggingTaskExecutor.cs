using Elastic.Apm.Api;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.Execution;

namespace MudBlazorUnited.Tasks.Demo;

[TaskExecutor("Loggers", 10)]
public class LoggingTaskExecutor : BaseTaskExecutor<LoggingTask, LoggingTaskConfig, LoggingTaskResult>
{
    public LoggingTaskExecutor(ILogger<LoggingTaskExecutor> logger, IServiceScopeFactory serviceScopeFactory,
        IRepository<LoggingTask, Guid> repository, ITracer? tracer = null) : base(logger, serviceScopeFactory,
        repository, tracer)
    {
    }

    protected override Task<LoggingTaskResult> ExecuteAsync(LoggingTask task, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Run Logging Task: {Id}", task.Config.Id);
        return Task.FromResult(new LoggingTaskResult());
    }
}
