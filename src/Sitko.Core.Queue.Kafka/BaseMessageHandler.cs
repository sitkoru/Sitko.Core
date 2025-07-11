using KafkaFlow;

namespace Sitko.Core.Queue.Kafka;

public abstract class BaseMessageHandler<TMessage> : IMessageHandler<TMessage>
{
    public Task Handle(IMessageContext context, TMessage message) =>
        Handle(message, context.ConsumerContext.WorkerStopped);

    protected abstract Task Handle(TMessage message, CancellationToken cancellationToken);
}
