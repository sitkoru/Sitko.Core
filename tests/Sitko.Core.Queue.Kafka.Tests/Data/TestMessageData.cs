using Sitko.Core.Queue.Tests;

namespace Sitko.Core.Queue.Kafka.Tests.Data;

public static class TestMessageData
{
    public static TestMessage Message { get; set; } = new()
    {
        Id = Guid.NewGuid()
    };

    public static QueueMessageContext MessageContext { get; set; } = new()
    {
        Id = Guid.NewGuid(),
        Date = DateTimeOffset.Now,
        MessageType = "Test",
        ReplyTo = Guid.NewGuid(),
        RequestId = Guid.NewGuid().ToString(),
        ParentMessageId = Guid.NewGuid(),
        RootMessageDate = DateTimeOffset.Now,
        RootMessageId = Guid.NewGuid()
    };
}
