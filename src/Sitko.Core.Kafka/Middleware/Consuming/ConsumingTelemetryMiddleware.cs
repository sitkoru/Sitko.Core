using KafkaFlow;
using Microsoft.Extensions.Logging;
using Sitko.Core.Kafka.Middleware.Producing;

namespace Sitko.Core.Kafka.Middleware.Consuming;

public class ConsumingTelemetryMiddleware(ILogger<ProducingTelemetryMiddleware> logger) : BaseKafkaMiddleware
{
    protected override bool HandleBefore(object message, IMessageContext context)
    {
        logger.LogDebug("Consuming message {Message} from topic {Topic}", message, context.ConsumerContext.Topic);
        return false;
    }

    protected override void HandleSuccess(object message, IMessageContext context) =>
        logger.LogDebug("Consumed message {Message} from topic {Topic}", message, context.ConsumerContext.Topic);
}
