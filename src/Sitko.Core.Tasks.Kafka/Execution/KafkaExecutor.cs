using KafkaFlow;
using KafkaFlow.TypedHandler;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Execution;

namespace Sitko.Core.Tasks.Kafka.Execution;

public class KafkaExecutor<TTask, TExecutor> : IMessageHandler<TTask> where TTask : class, IBaseTask where TExecutor : ITaskExecutor<TTask>
{
    private readonly TExecutor taskExecutor;

    public KafkaExecutor(TExecutor taskExecutor) => this.taskExecutor = taskExecutor;

    public async Task Handle(IMessageContext context, TTask message) =>
        await taskExecutor.ExecuteAsync(message.Id, CancellationToken.None);
}
