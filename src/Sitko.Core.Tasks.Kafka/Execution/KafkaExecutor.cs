using Microsoft.Extensions.Logging;
using Sitko.Core.Queue.Kafka.Consumption;
using Sitko.Core.Tasks.Data.Entities;
using Sitko.Core.Tasks.Execution;

namespace Sitko.Core.Tasks.Kafka.Execution;

public class KafkaExecutor<TTask, TExecutor> : BaseMessageHandler<TTask> where TTask : class, IBaseTask where TExecutor : ITaskExecutor<TTask>
{
    private readonly TExecutor taskExecutor;

    public KafkaExecutor(TExecutor taskExecutor, ILogger<BaseMessageHandler<TTask>> logger) : base(logger) => this.taskExecutor = taskExecutor;

    public override async Task HandleAsync(TTask @event) =>
        await taskExecutor.ExecuteAsync(@event.Id, CancellationToken.None);
}
