using KafkaFlow;
using KafkaFlow.Middlewares.Serializer.Resolvers;

namespace Sitko.Core.Queue.Kafka;

internal class EventTypeIdTypeResolver(KafkaMetadata metadata) : IMessageTypeResolver
{
    private const string EventTypeIdHeaderName = "EventTypeId";

    public ValueTask<Type> OnConsumeAsync(IMessageContext context)
    {
        var typeId = context.Headers.GetString(EventTypeIdHeaderName);
        var messageMetadata = metadata.GetByTypeId(typeId);

        return ValueTask.FromResult(messageMetadata.EventType);
    }

    public ValueTask OnProduceAsync(IMessageContext context)
    {
        var messageMetadata = metadata.GetByType(context.Message.Value.GetType());
        context.Headers.SetString(
            EventTypeIdHeaderName,
            messageMetadata.EventTypeId
        );
        return ValueTask.CompletedTask;
    }
}
