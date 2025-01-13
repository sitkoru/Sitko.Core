using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Tracing;
using TaskStatus = Sitko.Core.Tasks.Data.Entities.TaskStatus;

namespace Sitko.Core.Tasks.Execution;

public abstract class BaseTaskExecutor<TTask, TConfig, TResult> : ITaskExecutor<TTask, TConfig, TResult>,
    IDisposable
    where TTask : class, IBaseTask<TConfig, TResult>
    where TConfig : BaseTaskConfig, new()
    where TResult : BaseTaskResult, new()
{
    private readonly CancellationTokenSource activityTaskCts = new();
    private readonly IRepository<TTask, Guid> repository;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private Task? activityTask;

    protected BaseTaskExecutor(ITaskExecutorContext<TTask> executorContext,
        ILogger<BaseTaskExecutor<TTask, TConfig, TResult>> logger)
    {
        Logger = logger;
        serviceScopeFactory = executorContext.ServiceScopeFactory;
        repository = executorContext.Repository;
    }

    protected ILogger<BaseTaskExecutor<TTask, TConfig, TResult>> Logger { get; }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("TaskId", id.ToString()))
        using (LogContext.PushProperty("TaskType", typeof(TTask).Name))
        {
            try
            {
                using var activity = TracingHelper.ActivitySource.StartActivity($"Tasks/{typeof(TTask)}");
                activity?.SetTag("jobId", id.ToString());
                var task = await ExecuteTaskAsync(id, cancellationToken);
                if (task.TaskStatus == TaskStatus.Fails)
                {
                    throw new JobFailedException(id, typeof(TTask).Name,
                        task.Result?.ErrorMessage ?? "Unknown error");
                }
            }
            catch (JobFailedException)
            {
                Logger.LogDebug("Mark transaction as failed");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Task {JobId} ( {JobType} ) failed: {ErrorText}", id, typeof(TTask).Name,
                    ex.ToString());
            }
        }
    }

    private async Task<TTask> ExecuteTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Start job {JobId}", id);
        var task = await repository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            throw new InvalidOperationException($"Task {id} not found");
        }

        if (task.TaskStatus != TaskStatus.Wait)
        {
            var groupInfo = TaskExecutorHelper.GetGroupInfo(GetType());
            if (groupInfo is not { allowRetry: true })
            {
                Logger.LogInformation("Skip retry task {JobId}", id);
                return task;
            }

            Logger.LogInformation("Retry task {JobId}", id);
        }

        Logger.LogInformation("Set job {JobId} status to in progress", id);
        task.ExecuteDateStart = DateTimeOffset.UtcNow;
        task.TaskStatus = TaskStatus.InProgress;
        task.LastActivityDate = DateTimeOffset.UtcNow;
        await repository.UpdateAsync(task, cancellationToken);

        TResult result;
        TaskStatus status;
        activityTask = Task.Run(async () =>
        {
            while (!activityTaskCts.IsCancellationRequested)
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var scopedRepository = scope.ServiceProvider.GetRequiredService<IRepository<TTask, Guid>>();
                var scopedTask = await scopedRepository.GetByIdAsync(id, CancellationToken.None);
                if (scopedTask is not null)
                {
                    scopedTask.LastActivityDate = DateTimeOffset.UtcNow;
                    await scopedRepository.UpdateAsync(scopedTask, CancellationToken.None);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), activityTaskCts.Token);
                }
                catch (TaskCanceledException)
                {
                    Logger.LogDebug("Activity task was cancelled");
                }
            }
        }, activityTaskCts.Token);
        try
        {
            Logger.LogInformation("Try to execute job {JobId}", id);
            result = await ExecuteAsync(task, cancellationToken);
            if (result.IsSuccess)
            {
                Logger.LogInformation("Job {JobId} executed successfully", id);
                status = result.HasWarnings
                    ? TaskStatus.SuccessWithWarnings
                    : TaskStatus.Success;
            }
            else
            {
                Logger.LogInformation("Job {JobId} execution failed", id);
                status = TaskStatus.Fails;
            }
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex, "Job {JobId} execution failed with error: {ErrorText}", id, ex.ToString());
            status = TaskStatus.Fails;
            result = new TResult { IsSuccess = false, ErrorMessage = ex.Message };
        }

        await activityTaskCts.CancelAsync();
        try
        {
            await activityTask;
        }
        catch (OperationCanceledException)
        {
            // do nothing
        }
        catch (Exception ex)
        {
            Logger.LogInformation(ex, "Activity task error: {ErrorText}", ex.ToString());
        }

        Logger.LogInformation("Set job {JobId} result and save", id);
        task = await repository.RefreshAsync(task, cancellationToken);
        task.Result = result;
        task.TaskStatus = status;
        task.ExecuteDateEnd = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(task, cancellationToken);
        Logger.LogInformation("Job {JobId} finished", id);
        return task;
    }

    protected abstract Task<TResult> ExecuteAsync(TTask task, CancellationToken cancellationToken);

    public void Dispose()
    {
        activityTaskCts.Cancel();
        try
        {
            activityTask?.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in activity task: {ErrorText}", ex.ToString());
        }

        GC.SuppressFinalize(this);
    }
}

public record ExecutorRegistration(
    Type ExecutorType,
    Type EventType,
    string GroupId,
    int ParallelThreadCount,
    int BufferSize);
