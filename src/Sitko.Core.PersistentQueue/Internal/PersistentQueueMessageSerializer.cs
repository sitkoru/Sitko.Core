using System;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Sitko.Core.PersistentQueue.Queue;

namespace Sitko.Core.PersistentQueue.Internal
{
    public class PersistentQueueMessageSerializer
    {
        public QueueMsg Deserialize(byte[] data)
        {
            var message = new QueueMsg();
            message.MergeFrom(data);
            return message;
        }

        public byte[] Serialize(QueueMsg data)
        {
            return data.ToByteArray();
        }

        public QueueMsg Create(IMessage message, PersistentQueueMessageContext context = null)
        {
            var queueMessage = new QueueMsg
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTimeOffset.Now.ToTimestamp(),
                MessageType = message.Descriptor.FullName,
                Payload = Any.Pack(message)
            };
            queueMessage.SetContext(context);

            return queueMessage;
        }
    }
}
