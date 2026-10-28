using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Tasks.Components;
using Sitko.Core.Tasks.Execution;
using Sitko.Core.Tasks.Data.Repository;
using Sitko.Core.Tasks.Kafka.Tests.Data;
using Xunit;
using TaskStatus = Sitko.Core.Tasks.Data.Entities.TaskStatus;

namespace Sitko.Core.Tasks.Kafka.Tests
{
    public class KafkaTaskLifecycleTests(ITestOutputHelper testOutputHelper) : BaseKafkaTasksTest(testOutputHelper)
    {
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
