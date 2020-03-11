using System;

namespace Sitko.Core.Queue
{
    public class QueueMessageContext
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
        public string? MessageType { get; set; }
        public Guid? ParentMessageId { get; set; }
        public Guid? RootMessageId { get; set; }
        public DateTimeOffset? RootMessageDate { get; set; }
        public string? RequestId { get; set; }
        public Guid? ReplyTo { get; set; }
    }
}
