using Microsoft.Extensions.Logging;
using Sitko.Core.Tasks.Execution;

namespace Sitko.Core.Tasks.Kafka.Tests.Data
{
    [TaskExecutor("lifecycle", 1)]
    public class TestTaskExecutor(ITaskExecutorContext<TestTask> executorContext, ILogger<TestTaskExecutor> logger,
        TaskExecutionControl control)
        : BaseTaskExecutor<TestTask, TestTaskConfig, TestTaskResult>(executorContext, logger)
    {
        protected override async Task<TestTaskResult> ExecuteAsync(TestTask task, CancellationToken cancellationToken)
        {
            control.RecordExecution();
            _ = control.Entered.TrySetResult();
            await control.Release.Task.WaitAsync(cancellationToken);
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
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ExecutionCount => Volatile.Read(ref executionCount);
        public void RecordExecution() => Interlocked.Increment(ref executionCount);
    }
}
