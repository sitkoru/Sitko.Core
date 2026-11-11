using System.Diagnostics;
using KafkaFlow.Consumers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitko.Core.Tasks.Components;
using Sitko.Core.Tasks.Execution;
using Sitko.Core.Tasks.Kafka.Execution;
using Sitko.Core.Tasks.Data.Repository;
using Sitko.Core.Tasks.Kafka.Tests.Data;
using Xunit;
using TaskStatus = Sitko.Core.Tasks.Data.Entities.TaskStatus;

namespace Sitko.Core.Tasks.Kafka.Tests
{
    public class KafkaTaskLifecycleTests(ITestOutputHelper testOutputHelper) : BaseKafkaTasksTest(testOutputHelper)
    {
        [Fact]
        public async Task DirectExecutionWithCancelledTokenLeavesWaitingTaskUnchanged()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var task = new TestTask { Id = Guid.NewGuid(), Config = new TestTaskConfig() };
            var response = await scope.GetService<ITaskRepository<TestTask>>().AddAsync(task, cancellationToken);
            Assert.True(response.IsSuccess, response.ErrorsString);
            using var cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();

            await scope.GetService<ITaskExecutor<TestTask>>().ExecuteAsync(task.Id, cancelled.Token);

            var unchanged = await ReadTaskAsync(scope, task.Id, cancellationToken);
            Assert.Equal(TaskStatus.Wait, unchanged.TaskStatus);
            Assert.Null(unchanged.Result);
            Assert.Equal(0, scope.GetService<TaskExecutionControl>().ExecutionCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AcceptedTaskCancelledBeforeBusinessExecutionPersistsFailedResult(bool duringInitialUpdate)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            var executions = scope.GetService<KafkaTaskExecutions>();
            var task = new TestTask { Id = Guid.NewGuid(), Config = new TestTaskConfig() };
            var response = await scope.GetService<ITaskRepository<TestTask>>().AddAsync(task, cancellationToken);
            Assert.True(response.IsSuccess, response.ErrorsString);
            void Stop() => executions.StoppingAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (duringInitialUpdate)
            {
                control.BeforeInitialUpdate = Stop;
            }
            else
            {
                control.BeforeExecution = Stop;
            }

            var handler = ActivatorUtilities.CreateInstance<KafkaExecutor<TestTask, TestTaskExecutor>>(scope.Host.Services);
            await handler.Handle(new TestMessageContext(), task);
            var completed = await ReadTaskAsync(scope, task.Id, cancellationToken);
            Assert.Equal(TaskStatus.Fails, completed.TaskStatus);
            Assert.Equal("Task execution was cancelled", completed.Result?.ErrorMessage);
            _ = Assert.NotNull(completed.ExecuteDateStart);
            _ = Assert.NotNull(completed.ExecuteDateEnd);
            Assert.Equal(0, control.ExecutionCount);
        }

        [Fact]
        public async Task MessageReceivedAfterShutdownIsNotExecutedOrAcknowledged()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var task = new TestTask { Id = Guid.NewGuid(), Config = new TestTaskConfig() };
            var response = await scope.GetService<ITaskRepository<TestTask>>().AddAsync(task, cancellationToken);
            Assert.True(response.IsSuccess, response.ErrorsString);
            await scope.Host.StopAsync(cancellationToken);
            var context = new TestMessageContext();
            var handler = ActivatorUtilities.CreateInstance<KafkaExecutor<TestTask, TestTaskExecutor>>(scope.Host.Services);
            await handler.Handle(context, task);
            Assert.False(context.ShouldStoreOffset);
            Assert.Equal(0, scope.GetService<TaskExecutionControl>().ExecutionCount);
            var unchanged = await ReadTaskAsync(scope, task.Id, cancellationToken);
            Assert.Equal(TaskStatus.Wait, unchanged.TaskStatus);
            Assert.Null(unchanged.ExecuteDateStart);
            Assert.Null(unchanged.Result);
        }

        [Fact]
        public async Task ConsumerStopKeepsRunningTaskAliveWithoutCancellingIt()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            control.IgnoreCancellation = true;
            var task = new TestTask { Id = Guid.NewGuid(), FooId = 7, Config = new TestTaskConfig() };
            try
            {
                var response = await scope.GetService<TasksManager>().RunAsync(task, cancellationToken: cancellationToken);
                Assert.True(response.IsSuccess, response.ErrorMessage);
                await control.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                var consumer = Assert.Single(scope.GetService<IConsumerAccessor>().All);
                await consumer.StopAsync().WaitAsync(TimeSpan.FromSeconds(45), cancellationToken);
                Assert.False(control.CancellationObserved.Task.IsCompleted);
                var running = await ReadTaskAsync(scope, task.Id, cancellationToken);
                Assert.Equal(TaskStatus.InProgress, running.TaskStatus);
                Assert.False(control.DependencyDisposed.Task.IsCompleted);
                var stopping = scope.Host.StopAsync(cancellationToken);
                await control.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                Assert.False(stopping.IsCompleted);
                _ = control.Release.TrySetResult();
                await stopping.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
                Assert.True(control.DependencyDisposed.Task.IsCompleted);
                var completed = await WaitForStatusAsync(scope, task.Id, TaskStatus.Success, cancellationToken);
                Assert.Equal(7, completed.Result?.Foo);
            }
            finally
            {
                _ = control.Release.TrySetResult();
            }
        }

        [Fact]
        public async Task HostShutdownPreservesSuccessfulResultAfterLateCancellation()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            control.IgnoreCancellation = true;
            var task = new TestTask { Id = Guid.NewGuid(), FooId = 7, Config = new TestTaskConfig() };
            try
            {
                var response = await scope.GetService<TasksManager>().RunAsync(task, cancellationToken: cancellationToken);
                Assert.True(response.IsSuccess, response.ErrorMessage);
                await control.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                var stopping = scope.Host.StopAsync(cancellationToken);
                await control.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                _ = control.Release.TrySetResult();
                await stopping.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
                var completed = await ReadTaskAsync(scope, task.Id, cancellationToken);
                Assert.Equal(TaskStatus.Success, completed.TaskStatus);
                Assert.Equal(7, completed.Result?.Foo);
            }
            finally
            {
                _ = control.Release.TrySetResult();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task HostShutdownHonoursDeadlineForTaskIgnoringCancellation(bool blockCancellationCallback)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            control.IgnoreCancellation = true;
            control.BlockCancellationCallback = blockCancellationCallback;
            var task = new TestTask { Id = Guid.NewGuid(), Config = new TestTaskConfig() };
            var response = await scope.GetService<TasksManager>().RunAsync(task, cancellationToken: cancellationToken);
            Assert.True(response.IsSuccess, response.ErrorMessage);
            try
            {
                await control.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                var timer = Stopwatch.StartNew();
                var error = await Record.ExceptionAsync(() => scope.Host.StopAsync(deadline.Token));
                Assert.NotNull(error);
                if (error is AggregateException aggregate)
                {
                    Assert.All(aggregate.Flatten().InnerExceptions,
                        static exception => _ = Assert.IsType<OperationCanceledException>(exception, exactMatch: false));
                }
                else
                {
                    _ = Assert.IsType<OperationCanceledException>(error, exactMatch: false);
                }
                Assert.True(timer.Elapsed < TimeSpan.FromSeconds(5));
                Assert.True(control.CancellationObserved.Task.IsCompleted);
                Assert.False(control.DependencyDisposed.Task.IsCompleted);
            }
            finally
            {
                _ = control.Release.TrySetResult();
                await control.DependencyDisposed.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }

        [Fact]
        public async Task HostShutdownCancelsRunningTaskAndWaitsForFailedResult()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            var task = new TestTask { Id = Guid.NewGuid(), Config = new TestTaskConfig() };
            try
            {
                var response = await scope.GetService<TasksManager>().RunAsync(task, cancellationToken: cancellationToken);
                Assert.True(response.IsSuccess, response.ErrorMessage);
                await control.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                var stopping = scope.Host.StopAsync(cancellationToken);
                await control.CancellationObserved.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                Assert.False(stopping.IsCompleted);
                _ = control.Release.TrySetResult();
                await stopping.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
                var completed = await ReadTaskAsync(scope, task.Id, cancellationToken);
                Assert.Equal(TaskStatus.Fails, completed.TaskStatus);
                Assert.False(completed.Result?.IsSuccess);
                Assert.Equal("Task execution was cancelled", completed.Result?.ErrorMessage);
                _ = Assert.NotNull(completed.ExecuteDateEnd);
            }
            finally
            {
                _ = control.Release.TrySetResult();
            }
        }

        [Fact]
        public async Task MessageScopeDisposalDoesNotDisposeRunningTask()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            var task = new TestTask { Id = Guid.NewGuid(), FooId = 7, Config = new TestTaskConfig() };
            var response = await scope.GetService<ITaskRepository<TestTask>>().AddAsync(task, cancellationToken);
            Assert.True(response.IsSuccess, response.ErrorsString);
            var messageScope = scope.CreateScope();
            var handler = ActivatorUtilities.CreateInstance<KafkaExecutor<TestTask, TestTaskExecutor>>(messageScope.ServiceProvider);
            var execution = handler.Handle(null!, task);
            try
            {
                await control.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                messageScope.Dispose();
                Assert.False(execution.IsCompleted);
                _ = control.Release.TrySetResult();
                await execution.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                var completed = await ReadTaskAsync(scope, task.Id, cancellationToken);
                Assert.Equal(TaskStatus.Success, completed.TaskStatus);
                Assert.Equal(7, completed.Result?.Foo);
            }
            finally
            {
                _ = control.Release.TrySetResult();
                messageScope.Dispose();
            }
        }

        [Fact]
        public async Task RunPersistsProgressResultAndSkipsRepeatedExecution()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            var parentId = Guid.NewGuid();
            var task = new TestTask { Id = Guid.NewGuid(), FooId = 7, Config = new TestTaskConfig() };
            var beforeRun = DateTimeOffset.UtcNow;
            try
            {
                var response = await scope.GetService<TasksManager>().RunAsync(task, parentId, "lifecycle-user", cancellationToken);
                Assert.True(response.IsSuccess, response.ErrorMessage);
                await control.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

                var running = await ReadTaskAsync(scope, task.Id, cancellationToken);
                Assert.Equal(TaskStatus.InProgress, running.TaskStatus);
                Assert.Equal(parentId, running.ParentId);
                Assert.Equal("lifecycle-user", running.UserId);
                _ = Assert.NotNull(running.ExecuteDateStart);
                Assert.InRange(running.ExecuteDateStart.Value, beforeRun, DateTimeOffset.UtcNow);
                _ = Assert.NotNull(running.LastActivityDate);
                Assert.Null(running.ExecuteDateEnd);
                Assert.Null(running.Result);
                _ = control.Release.TrySetResult();

                var completed = await WaitForStatusAsync(scope, task.Id, TaskStatus.Success, cancellationToken);
                Assert.Equal(parentId, completed.ParentId);
                Assert.Equal("lifecycle-user", completed.UserId);
                Assert.Equal(running.ExecuteDateStart, completed.ExecuteDateStart);
                _ = Assert.NotNull(completed.ExecuteDateEnd);
                Assert.InRange(completed.ExecuteDateEnd.Value, running.ExecuteDateStart.Value, DateTimeOffset.UtcNow);
                Assert.NotNull(completed.Result);
                Assert.True(completed.Result.IsSuccess);
                Assert.Equal(7, completed.Result.Foo);
                Assert.Equal(1, control.ExecutionCount);

                using (var retryScope = scope.CreateScope())
                {
                    await retryScope.ServiceProvider.GetRequiredService<ITaskExecutor<TestTask>>()
                        .ExecuteAsync(task.Id, cancellationToken);
                }

                var repeated = await ReadTaskAsync(scope, task.Id, cancellationToken);
                Assert.Equal(1, control.ExecutionCount);
                Assert.Equal(TaskStatus.Success, repeated.TaskStatus);
                Assert.Equal(completed.ExecuteDateStart, repeated.ExecuteDateStart);
                Assert.Equal(completed.ExecuteDateEnd, repeated.ExecuteDateEnd);
                Assert.Equal(completed.Result, repeated.Result);
            }
            finally
            {
                _ = control.Release.TrySetResult();
            }
        }

        [Fact]
        public async Task ExecutePersistsSuccessfulResultWithWarnings()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            _ = control.Release.TrySetResult();
            var task = new TestTask
            {
                Id = Guid.NewGuid(),
                FooId = 7,
                Config = new TestTaskConfig { HasWarnings = true }
            };
            var response = await scope.GetService<ITaskRepository<TestTask>>().AddAsync(task, cancellationToken);
            Assert.True(response.IsSuccess, response.ErrorsString);
            await scope.GetService<ITaskExecutor<TestTask>>().ExecuteAsync(task.Id, cancellationToken);

            var completed = await ReadTaskAsync(scope, task.Id, cancellationToken);
            Assert.Equal(TaskStatus.SuccessWithWarnings, completed.TaskStatus);
            Assert.NotNull(completed.Result);
            Assert.True(completed.Result.IsSuccess);
            Assert.True(completed.Result.HasWarnings);
            Assert.Equal(7, completed.Result.Foo);
            _ = Assert.NotNull(completed.ExecuteDateStart);
            _ = Assert.NotNull(completed.ExecuteDateEnd);
            Assert.True(completed.ExecuteDateEnd >= completed.ExecuteDateStart);
            Assert.Equal(1, control.ExecutionCount);
        }

        [Fact]
        public async Task ExecutePersistsBusinessExceptionAsFailedResult()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            _ = control.Release.TrySetResult();
            var task = new TestTask
            {
                Id = Guid.NewGuid(),
                FooId = 7,
                Config = new TestTaskConfig { Error = "business failure" }
            };
            var response = await scope.GetService<ITaskRepository<TestTask>>().AddAsync(task, cancellationToken);
            Assert.True(response.IsSuccess, response.ErrorsString);
            await scope.GetService<ITaskExecutor<TestTask>>().ExecuteAsync(task.Id, cancellationToken);

            var completed = await ReadTaskAsync(scope, task.Id, cancellationToken);
            Assert.Equal(TaskStatus.Fails, completed.TaskStatus);
            Assert.NotNull(completed.Result);
            Assert.False(completed.Result.IsSuccess);
            Assert.Equal("business failure", completed.Result.ErrorMessage);
            _ = Assert.NotNull(completed.ExecuteDateStart);
            _ = Assert.NotNull(completed.ExecuteDateEnd);
            Assert.True(completed.ExecuteDateEnd >= completed.ExecuteDateStart);
            Assert.Equal(1, control.ExecutionCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposeReportsActivityFailuresButNotItsCancellation(bool failActivity)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var scope = await GetScopeAsync();
            var control = scope.GetService<TaskExecutionControl>();
            var activityEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var failure = new InvalidOperationException("activity failure");
            control.DuringActivityUpdate = async token =>
            {
                _ = activityEntered.TrySetResult();
                if (failActivity)
                {
                    throw failure;
                }
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            };
            var task = new TestTask { Id = Guid.NewGuid(), Config = new TestTaskConfig() };
            var response = await scope.GetService<ITaskRepository<TestTask>>().AddAsync(task, cancellationToken);
            Assert.True(response.IsSuccess, response.ErrorsString);
            using var executionScope = scope.CreateScope();
            var logger = new ActivityTestLogger();
            var executor = ActivatorUtilities.CreateInstance<TestTaskExecutor>(executionScope.ServiceProvider, logger);
            try
            {
                var execution = executor.ExecuteAsync(task.Id, cancellationToken);
                await activityEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                _ = control.Release.TrySetResult();
                await execution.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                executor.Dispose();

                var completed = await ReadTaskAsync(scope, task.Id, cancellationToken);
                Assert.Equal(TaskStatus.Success, completed.TaskStatus);
                Assert.True(completed.Result?.IsSuccess);
                if (failActivity)
                {
                    Assert.Same(failure, Assert.Single(logger.Errors));
                }
                else
                {
                    Assert.Empty(logger.Errors);
                }
            }
            finally
            {
                _ = control.Release.TrySetResult();
            }
        }

        private sealed class ActivityTestLogger : ILogger<TestTaskExecutor>
        {
            public List<Exception?> Errors { get; } = [];
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (logLevel == LogLevel.Error)
                {
                    Errors.Add(exception);
                }
            }
        }

        private static async Task<TestTask> ReadTaskAsync(BaseKafkaTasksTestScope scope, Guid id,
            CancellationToken cancellationToken)
        {
            using var readScope = scope.CreateScope();
            var task = await readScope.ServiceProvider.GetRequiredService<TasksManager>()
                .GetByIdAsync<TestTask>(id, cancellationToken);
            Assert.NotNull(task);
            return task;
        }

        private static async Task<TestTask> WaitForStatusAsync(BaseKafkaTasksTestScope scope, Guid id,
            TaskStatus status, CancellationToken cancellationToken)
        {
            var timer = Stopwatch.StartNew();
            TestTask task;
            do
            {
                task = await ReadTaskAsync(scope, id, cancellationToken);
                if (task.TaskStatus == status)
                {
                    return task;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            } while (timer.Elapsed < TimeSpan.FromSeconds(30));

            Assert.Equal(status, task.TaskStatus);
            return task;
        }
    }
}
