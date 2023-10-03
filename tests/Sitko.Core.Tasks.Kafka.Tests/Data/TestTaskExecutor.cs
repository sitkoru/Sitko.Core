using Elastic.Apm.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.Execution;

namespace Sitko.Core.Tasks.Kafka.Tests.Data;

public class TestTaskExecutor : BaseTaskExecutor<TestTask, TestTaskConfig, TestTaskResult>
{
    public TestTaskExecutor(ILogger<TestTaskExecutor> logger, ITracer? tracer, IServiceScopeFactory serviceScopeFactory,
        IRepository<TestTask, Guid> repository) : base(logger, tracer, serviceScopeFactory, repository)
    {
    }

    protected override Task<TestTaskResult> ExecuteAsync(TestTask task, CancellationToken cancellationToken)
    {
        var result = new TestTaskResult { Id = Guid.NewGuid(), Foo = task.FooId };
        return Task.FromResult(result);
    }
}
