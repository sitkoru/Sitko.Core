namespace Sitko.Core.Tasks.Execution;

public interface ITaskExecutor
{
    Task ExecuteAsync(Guid id, CancellationToken cancellationToken);
}

public interface ITaskExecutor<TTask, TConfig, TResult> : ITaskExecutor
    where TTask : class, IBaseTask<TConfig, TResult>
    where TConfig : BaseTaskConfig, new()
    where TResult : BaseTaskResult, new()
{
}
