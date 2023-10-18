using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Kafka.Tests.Data;

public abstract record BaseTestTask : BaseTask
{
    public int FooId { get; set; }
}

public abstract record BaseTestTask<TConfig, TResult> : BaseTestTask, IBaseTask<TConfig, TResult>
    where TConfig : BaseTaskConfig, new() where TResult : BaseTaskResult
{
    public TConfig Config { get; set; } = default!;
    public TResult? Result { get; set; }
}
