using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Tasks.Scheduling;

namespace Sitko.Core.Tasks.Kafka.Tests.Data;

public class TestTaskScheduler : BaseTaskScheduler<TestTask>
{
    public TestTaskScheduler(IOptions<TaskSchedulingOptions<TestTask>> taskOptions,
        IOptionsMonitor<TasksModuleOptions> optionsMonitor, ITaskScheduler taskScheduler,
        ILogger<TestTaskScheduler> logger) : base(taskOptions, optionsMonitor, taskScheduler, logger)
    {
    }

    protected override Task<TestTask[]> GetTasksAsync(CancellationToken cancellationToken)
    {
        var tasks = new[] { new TestTask { Id = Guid.NewGuid(), FooId = 1 } };
        return Task.FromResult(tasks);
    }
}
