using KafkaFlow;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Kafka.Middleware.Producing;

public class ProducingTelemetryMiddleware(ILogger<ProducingTelemetryMiddleware> logger) : BaseKafkaMiddleware
{
    protected override bool HandleBefore(object message, IMessageContext context)
    {
        logger.LogDebug("Producing message {Message} to topic {Topic}", message, context.ProducerContext.Topic);
        return false;
    }

    protected override void HandleSuccess(object message, IMessageContext context) =>
        logger.LogDebug("Produced message {Message} to topic {Topic}", message, context.ProducerContext.Topic);
}
