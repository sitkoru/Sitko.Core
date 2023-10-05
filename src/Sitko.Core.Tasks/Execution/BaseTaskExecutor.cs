﻿using Elastic.Apm.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Sitko.Core.Repository;
using Sitko.Core.Tasks.Data.Entities;
using TaskStatus = Sitko.Core.Tasks.Data.Entities.TaskStatus;

namespace Sitko.Core.Tasks.Execution;

public abstract class BaseTaskExecutor<TTask, TConfig, TResult> : ITaskExecutor<TTask, TConfig, TResult>
    where TTask : class, IBaseTask<TConfig, TResult>
    where TConfig : BaseTaskConfig, new()
    where TResult : BaseTaskResult, new()
{
    private readonly ITracer? tracer;
    private readonly ILogger<BaseTaskExecutor<TTask, TConfig, TResult>> logger;
    private readonly CancellationTokenSource activityTaskCts = new();
    private readonly IRepository<TTask, Guid> repository;
    private readonly IServiceScopeFactory serviceScopeFactory;

    protected BaseTaskExecutor(ILogger<BaseTaskExecutor<TTask, TConfig, TResult>> logger,
        IServiceScopeFactory serviceScopeFactory, IRepository<TTask, Guid> repository, ITracer? tracer = null)
    {
        this.logger = logger;
        this.tracer = tracer;
        this.serviceScopeFactory = serviceScopeFactory;
        this.repository = repository;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("TaskId", id.ToString()))
        using (LogContext.PushProperty("TaskType", typeof(TTask).Name))
        {
            try
            {
                if (tracer is not null)
                {
                    await tracer.CaptureTransaction($"Tasks/{typeof(TTask)}", "Task", async transaction =>
                    {
                        transaction.SetLabel("jobId", id.ToString());
                        var task = await ExecuteTaskAsync(id, cancellationToken);
                        if (task.TaskStatus == TaskStatus.Fails)
                        {
                            throw new JobFailedException(id, typeof(TTask).Name,
                                task.Result?.ErrorMessage ?? "Unknown error");
                        }
                    });
                }
                else
                {
                    await ExecuteTaskAsync(id, cancellationToken);
                }
            }
            catch (JobFailedException)
            {
                logger.LogDebug("Mark transaction as failed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Task {JobId} ( {JobType} ) failed: {ErrorText}", id, typeof(TTask).Name,
                    ex.ToString());
            }
        }
    }

    private async Task<TTask> ExecuteTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Start job {JobId}", id);
        var task = await repository.GetByIdAsync(id, cancellationToken);
        if (task is null)
        {
            throw new InvalidOperationException($"Task {id} not found");
        }

        if (task.TaskStatus != TaskStatus.Wait)
        {
            logger.LogInformation("Skip retry task {JobId}", id);
            return task;
        }

        logger.LogInformation("Set job {JobId} status to in progress", id);
        task.ExecuteDateStart = DateTimeOffset.UtcNow;
        task.TaskStatus = TaskStatus.InProgress;
        task.LastActivityDate = DateTimeOffset.UtcNow;
        await repository.UpdateAsync(task, cancellationToken);

        TResult result;
        TaskStatus status;
        var activityTask = Task.Run(async () =>
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

                await Task.Delay(TimeSpan.FromSeconds(5), activityTaskCts.Token);
            }
        }, activityTaskCts.Token);
        try
        {
            logger.LogInformation("Try to execute job {JobId}", id);
            result = await ExecuteAsync(task, cancellationToken);
            if (result.IsSuccess)
            {
                logger.LogInformation("Job {JobId} executed successfully", id);
                status = result.HasWarnings
                    ? TaskStatus.SuccessWithWarnings
                    : TaskStatus.Success;
            }
            else
            {
                logger.LogInformation("Job {JobId} execution failed", id);
                status = TaskStatus.Fails;
            }
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Job {JobId} execution failed with error: {ErrorText}", id, ex.ToString());
            status = TaskStatus.Fails;
            result = new TResult { IsSuccess = false, ErrorMessage = ex.Message };
        }

        activityTaskCts.Cancel();
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
            logger.LogInformation(ex, "Activity task error: {ErrorText}", ex.ToString());
        }

        logger.LogInformation("Set job {JobId} result and save", id);
        task = await repository.RefreshAsync(task, cancellationToken);
        task.Result = result;
        task.TaskStatus = status;
        task.ExecuteDateEnd = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(task, cancellationToken);
        logger.LogInformation("Job {JobId} finished", id);
        return task;
    }

    protected abstract Task<TResult> ExecuteAsync(TTask task, CancellationToken cancellationToken);
}

public record ExecutorRegistration(Type ExecutorType, Type EventType, string GroupId, int ParallelThreadCount,
    int BufferSize);
