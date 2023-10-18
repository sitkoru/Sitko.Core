using Sitko.Core.Tasks.Data.Entities;

namespace Sitko.Core.Tasks.Execution;

public interface ITaskExecutor
{
    Task ExecuteAsync(Guid id, CancellationToken cancellationToken);
}

// ReSharper disable once UnusedTypeParameter
public interface ITaskExecutor<TTask>: ITaskExecutor where TTask : class, IBaseTask
{
}

public interface ITaskExecutor<TTask, TConfig, TResult> : ITaskExecutor<TTask>
    where TTask : class, IBaseTask<TConfig, TResult>
    where TConfig : BaseTaskConfig, new()
    where TResult : BaseTaskResult, new()
{
}
