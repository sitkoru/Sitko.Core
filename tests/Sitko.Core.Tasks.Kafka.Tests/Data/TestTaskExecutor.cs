using Microsoft.Extensions.Logging;
using Sitko.Core.Tasks.Execution;

namespace Sitko.Core.Tasks.Kafka.Tests.Data
{
    [TaskExecutor("lifecycle", 1)]
    public class TestTaskExecutor(ITaskExecutorContext<TestTask> executorContext, ILogger<TestTaskExecutor> logger,
        TaskExecutionControl control, TaskDependency dependency)
        : BaseTaskExecutor<TestTask, TestTaskConfig, TestTaskResult>(executorContext, logger)
    {
        protected override async Task<TestTaskResult> ExecuteAsync(TestTask task, CancellationToken cancellationToken)
        {
            control.RecordExecution();
            using var registration = cancellationToken.Register(() =>
            {
                _ = control.CancellationObserved.TrySetResult();
                if (control.BlockCancellationCallback)
                {
                    control.Release.Task.GetAwaiter().GetResult();
                }
            });
            _ = control.Entered.TrySetResult();
            try
            {
                await control.Release.Task.WaitAsync(control.IgnoreCancellation ? CancellationToken.None : cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await control.Release.Task;
                throw;
            }
            dependency.ThrowIfDisposed();
            if (task.Config.Error is { } error)
            {
                throw new InvalidOperationException(error);
            }

            return new TestTaskResult { Id = Guid.NewGuid(), Foo = task.FooId, HasWarnings = task.Config.HasWarnings };
        }
    }

    public sealed class TaskExecutionControl
    {
        private int executionCount;
        private int updateCount;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource CancellationObserved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource DependencyDisposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IgnoreCancellation { get; set; }
        public bool BlockCancellationCallback { get; set; }
        public Action? BeforeExecution { get; set; }
        public Action? BeforeInitialUpdate { get; set; }
        public Func<CancellationToken, Task>? DuringActivityUpdate { get; set; }
        public int RecordUpdate() => Interlocked.Increment(ref updateCount);
        public int ExecutionCount => Volatile.Read(ref executionCount);
        public void RecordExecution() => Interlocked.Increment(ref executionCount);
    }

    public sealed class TaskDependency : IDisposable
    {
        private readonly TaskExecutionControl control;
        private bool disposed;

        public TaskDependency(TaskExecutionControl control)
        {
            this.control = control;
            control.BeforeExecution?.Invoke();
        }

        public void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, this);
        public void Dispose()
        {
            disposed = true;
            _ = control.DependencyDisposed.TrySetResult();
        }
    }
}
