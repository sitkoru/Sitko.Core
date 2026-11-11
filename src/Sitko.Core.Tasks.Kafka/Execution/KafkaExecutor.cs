using KafkaFlow;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Execution;

namespace Sitko.Core.Tasks.Kafka.Execution;

public class KafkaExecutor<TTask, TExecutor>(IServiceScopeFactory scopeFactory, KafkaTaskExecutions executions) : IMessageHandler<TTask> where TTask : class, IBaseTask where TExecutor : ITaskExecutor<TTask>
{
    public async Task Handle(IMessageContext context, TTask message)
    {
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!executions.TryRegister(finished.Task))
        {
            context.ConsumerContext.ShouldStoreOffset = false;
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var taskExecutor = scope.ServiceProvider.GetRequiredService<TExecutor>();
            await taskExecutor.ExecuteAsync(message.Id, executions.StoppingToken, executions.CompletionToken);
        }
        finally
        {
            finished.SetResult();
            executions.Complete(finished.Task);
        }
    }
}
