using System.Text;
using KafkaFlow;
using Timestamp=Google.Protobuf.WellKnownTypes.Timestamp;

namespace Sitko.Core.Queue.Kafka;

public interface IQueueConsumer<TMessage> : IMessageHandler<TMessage>;

public abstract class BaseQueueConsumer<TMessage> : IQueueConsumer<TMessage>
{
    public abstract Task Handle(IMessageContext context, TMessage message);

    protected QueueMessageContext DeserializeMessageHeaders(IMessageHeaders headers)
    {
        var messageContext = new QueueMessageContext();

        var id = GetHeaderValueByKey(headers, nameof(messageContext.Id));
        messageContext.Id = id is { Length: 16 } ? new Guid(id) : Guid.Empty;

        var parentId = GetHeaderValueByKey(headers, nameof(messageContext.ParentMessageId));
        messageContext.ParentMessageId = parentId is { Length: 16 } ? new Guid(parentId) : null;

        var rootMessageId = GetHeaderValueByKey(headers, nameof(messageContext.RootMessageId));
        messageContext.RootMessageId = rootMessageId is { Length: 16 } ? new Guid(rootMessageId) : null;

        var rootMessageDate = GetHeaderValueByKey(headers, nameof(messageContext.RootMessageDate));
        messageContext.RootMessageDate = rootMessageDate != null ? Timestamp.Parser.ParseFrom(rootMessageDate).ToDateTimeOffset() : null;

        var requestId = GetHeaderValueByKey(headers, nameof(messageContext.RequestId));
        messageContext.RequestId = requestId != null ? Encoding.UTF8.GetString(requestId) : null;

        var messageType = GetHeaderValueByKey(headers, nameof(messageContext.MessageType));
        messageContext.MessageType = messageType != null ? Encoding.UTF8.GetString(messageType) : null;

        var date = GetHeaderValueByKey(headers, nameof(messageContext.Date));
        messageContext.Date = date != null ?  Timestamp.Parser.ParseFrom(date).ToDateTimeOffset() : new DateTimeOffset();

        var replyTo = GetHeaderValueByKey(headers, nameof(messageContext.ReplyTo));
        messageContext.ReplyTo = replyTo is { Length: 16 } ? new Guid(replyTo) : null;

        return messageContext;
    }

    private static byte[]? GetHeaderValueByKey(IMessageHeaders headers, string key) =>
        headers.FirstOrDefault(header => header.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;
}

