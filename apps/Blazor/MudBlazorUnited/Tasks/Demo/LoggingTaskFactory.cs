using Sitko.Core.Tasks.Scheduling;

namespace MudBlazorUnited.Tasks.Demo;

public class LoggingTaskFactory : IBaseTaskFactory<LoggingTask>
{
    public Task<LoggingTask[]> GetTasksAsync(CancellationToken cancellationToken)
    {
        var tasks = new[] { new LoggingTask { Config = new() { Id = Guid.NewGuid() }, Result = new() } };
        return Task.FromResult(tasks);
    }
}
