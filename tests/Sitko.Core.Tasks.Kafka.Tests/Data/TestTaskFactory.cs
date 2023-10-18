using Sitko.Core.Tasks.Scheduling;

namespace Sitko.Core.Tasks.Kafka.Tests.Data;

public class TestTaskFactory : IBaseTaskFactory<TestTask>
{
    public Task<TestTask[]> GetTasksAsync(CancellationToken cancellationToken)
    {
        var tasks = new[] { new TestTask { Id = Guid.NewGuid(), FooId = 1 } };
        return Task.FromResult(tasks);
    }
}
