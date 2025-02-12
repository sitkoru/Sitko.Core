using JetBrains.Annotations;
using KafkaFlow;
using Microsoft.Extensions.Logging;
using Sitko.Core.Queue.Kafka.Events;

namespace Sitko.Core.Queue.Kafka.Consumption;

public abstract class BaseMessageHandler<TEvent>
    (ILogger<BaseMessageHandler<TEvent>> logger)
    : IMessageHandler<TEvent>
    where TEvent : BaseEvent
{
    public async Task Handle(IMessageContext context, TEvent message) =>
        await HandleAsync(message).ConfigureAwait(false);

    [PublicAPI]
    public abstract Task HandleAsync(TEvent @event);
}
