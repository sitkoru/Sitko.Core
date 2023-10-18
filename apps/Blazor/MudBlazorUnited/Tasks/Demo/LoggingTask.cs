using Sitko.Core.Tasks;
using Sitko.Core.Tasks.Data.Entities;

namespace MudBlazorUnited.Tasks.Demo;

[Task("logging")]
public record LoggingTask : MudBlazorBaseTask<LoggingTaskConfig, LoggingTaskResult>;

public record LoggingTaskResult : BaseTaskResult;

public record LoggingTaskConfig : BaseTaskConfig
{
    public Guid Id { get; set; }
}
