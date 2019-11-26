using System;

namespace Sitko.Core.PersistentQueue
{
    public class PersistentQueueMessageContext
    {
        public Guid MessageId { get; }
        public Guid? ParentMessageId { get; set; }
        public Guid? RootMessageId { get; set; }
        public DateTimeOffset? RootMessageDate { get; set; }
        public string RequestId { get; }

        public PersistentQueueMessageContext(string requestId)
        {
            RequestId = requestId;
        }

        public PersistentQueueMessageContext(Guid id, string requestId) : this(requestId)
        {
            MessageId = id;
        }
    }
}
