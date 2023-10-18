using Microsoft.Extensions.Logging;
using Sitko.Core.Tasks.Execution;

namespace Sitko.Core.Tasks.Kafka.Tests.Data;

public class TestTaskExecutor : BaseTaskExecutor<TestTask, TestTaskConfig, TestTaskResult>
{
    public TestTaskExecutor(ITaskExecutorContext<TestTask> executorContext, ILogger<TestTaskExecutor> logger) : base(
        executorContext, logger)
    {
    }

    protected override Task<TestTaskResult> ExecuteAsync(TestTask task, CancellationToken cancellationToken)
    {
        var result = new TestTaskResult { Id = Guid.NewGuid(), Foo = task.FooId };
        return Task.FromResult(result);
    }
}
