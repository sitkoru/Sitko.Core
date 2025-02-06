using KafkaFlow;
using Sitko.Core.Kafka;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Sitko.Core.Queue.Tests;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests;

[KafkaConsumer("Test", 10)]
public class KafkaQueueTestConsumer : BaseQueueConsumer<TestMessage>
{
    public override Task Handle(IMessageContext context, TestMessage message)
    {
        var headers = DeserializeMessageHeaders(context.Headers);
        Assert.Equal(headers, TestMessageData.MessageContext);
        Assert.Equal(message, TestMessageData.Message);
        return Task.CompletedTask;
    }
}
