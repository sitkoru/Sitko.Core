using System.Text.Json;
using KafkaFlow;
using Microsoft.Extensions.Logging;
using Sitko.Core.Queue.Kafka.Events;

namespace Sitko.Core.Queue.Kafka.Middleware.Producing;

public class EventsProducingLogger
    (ILogger<EventsProducingLogger> logger)
    : MessageMiddlewareBase
{
    protected override bool HandleBefore(BaseEvent message, IMessageContext context)
    {
        base.HandleBefore(message, context);
        logger.LogInformation(
        "Producing event of type {MessageType} with key {MessageKey} to topic {Topic} started",
        message.GetType().Name,
        GetMessageKey(context), context.ProducerContext.Topic);
        return true;
    }

    protected override void HandleFail(Exception exception, BaseEvent message, IMessageContext context)
    {
        base.HandleFail(exception, message, context);

        exception.Data["AdditionalInfo"] = JsonSerializer.Serialize(message, message.GetType(), JsonSerializerOptions);

        logger.LogError(exception,
        "An error occurred while processing the event. Type of event: {MessageType}. Event key: {MessageKey}",
        message.GetType().FullName, GetMessageKey(context));
    }

    protected override void HandleFinally(BaseEvent message, IMessageContext context)
    {
        base.HandleFinally(message, context);
        logger.LogInformation(
        "Producing event of type {MessageType} with key {MessageKey} to topic {Topic} completed",
        message.GetType().Name,
        GetMessageKey(context), context.ProducerContext.Topic);
    }
}
