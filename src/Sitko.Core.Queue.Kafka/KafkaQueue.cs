using System.Text;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using KafkaFlow;
using KafkaFlow.Producers;

namespace Sitko.Core.Queue.Kafka;

public class KafkaQueue(IProducerAccessor producerAccessor)
{
    public async Task<QueuePublishResult> PublishAsync<T>(T message, QueueMessageContext messageContext)
    {
        var publishResult = new QueuePublishResult();
        try
        {
            var producer = producerAccessor[KafkaQueueHelper.GetProducerName(typeof(T))];

            var headers = SerializeMessageHeaders(messageContext);
            await producer.ProduceAsync
                (messageKey: messageContext.Id.ToString(),
                messageValue: message,
                headers: headers);
        }
        catch (Exception e)
        {
            publishResult.SetException(e);
        }

        return publishResult;
    }

    private static MessageHeaders SerializeMessageHeaders(QueueMessageContext messageContext) =>
        new()
        {
            { nameof(messageContext.Id), messageContext.Id.ToByteArray() },
            { nameof(messageContext.ParentMessageId), messageContext.ParentMessageId != null
                ? messageContext.ParentMessageId.Value.ToByteArray() : [] },
            { nameof(messageContext.RootMessageId), messageContext.RootMessageId != null
                ? messageContext.RootMessageId.Value.ToByteArray() : [] },
            { nameof(messageContext.RootMessageDate), messageContext.RootMessageDate != null
                ? messageContext.RootMessageDate.Value.ToTimestamp().ToByteArray() : [] },
            { nameof(messageContext.Date), messageContext.Date.ToTimestamp().ToByteArray() },
            { nameof(messageContext.MessageType), !string.IsNullOrEmpty(messageContext.MessageType)
                ? Encoding.UTF8.GetBytes(messageContext.MessageType) : [] },
            { nameof(messageContext.RequestId), !string.IsNullOrEmpty(messageContext.RequestId)
                ? Encoding.UTF8.GetBytes(messageContext.RequestId) : [] },
            { nameof(messageContext.ReplyTo),  messageContext.ReplyTo != null
                ? messageContext.ReplyTo.Value.ToByteArray() : [] }
        };
}
