using KafkaFlow;

namespace Sitko.Core.Queue.Kafka;

public interface IBatchMessageHandler;

public interface IBatchMessageHandler<in TMessage> : IBatchMessageHandler;

public abstract class BaseBatchMessageHandler<TMessage> : IMessageMiddleware, IBatchMessageHandler<TMessage>
{
    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        var batch = context.GetMessagesBatch();

        var messages = batch.Select(b => b.Message.Value).OfType<TMessage>().ToArray();

        await HandleAsync(messages, context.ConsumerContext.WorkerStopped);
    }

    protected abstract Task HandleAsync(TMessage[] messages, CancellationToken cancellationToken);
}
