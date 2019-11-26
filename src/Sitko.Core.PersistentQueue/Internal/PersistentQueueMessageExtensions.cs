using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Sitko.Core.PersistentQueue.Queue;

namespace Sitko.Core.PersistentQueue.Internal
{
    public static class PersistentQueueMessageExtensions
    {
        public static string GetQueueName(this IMessage message)
        {
            return message.Descriptor.FullName;
        }

        public static T GetMessage<T>(this QueueMsg queueMessage) where T : IMessage, new()
        {
            return queueMessage.Payload.Unpack<T>();
        }

        public static PersistentQueueMessageContext GetContext(this QueueMsg queueMessage)
        {
            return new PersistentQueueMessageContext(Guid.Parse(queueMessage.Id), queueMessage.RequestId)
            {
                RootMessageId =
                    string.IsNullOrEmpty(queueMessage.RootMessageId)
                        ? Guid.Parse(queueMessage.Id)
                        : Guid.Parse(queueMessage.RootMessageId),
                RootMessageDate =
                    queueMessage.RootMessageDate?.ToDateTimeOffset() ?? queueMessage.Date.ToDateTimeOffset(),
                ParentMessageId = Guid.Parse(queueMessage.Id)
            };
        }

        public static void SetContext(this QueueMsg queueMessage, PersistentQueueMessageContext context = null)
        {
            if (context == null) return;

            if (context.RootMessageId.HasValue)
            {
                queueMessage.RootMessageId = context.RootMessageId.ToString();
            }

            if (context.RootMessageDate.HasValue)
            {
                queueMessage.RootMessageDate = context.RootMessageDate.Value.ToTimestamp();
            }

            if (context.ParentMessageId.HasValue)
            {
                queueMessage.ParentMessageId = context.ParentMessageId.ToString();
            }

            if (!string.IsNullOrEmpty(context.RequestId))
            {
                queueMessage.RequestId = context.RequestId;
            }
        }
    }
}
