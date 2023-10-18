using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Kafka.Tests.Data;

public record TestTask : BaseTestTask<TestTaskConfig, TestTaskResult>;

public record TestTaskResult : BaseTaskResult
{
    public int Foo { get; init; }
    public Guid Id { get; init; }
}

public record TestTaskConfig : BaseTaskConfig;
