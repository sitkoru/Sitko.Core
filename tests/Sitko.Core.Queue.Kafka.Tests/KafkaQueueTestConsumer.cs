using Microsoft.Extensions.Logging;
using Sitko.Core.Queue.Kafka.Attributes;
using Sitko.Core.Queue.Kafka.Consumption;
using Sitko.Core.Queue.Kafka.Tests.Data;
using Xunit;

namespace Sitko.Core.Queue.Kafka.Tests;

[MessageHandler("Test", 10)]
public class KafkaQueueTestConsumer(ILogger<BaseMessageHandler<TestEvent>> logger) : BaseMessageHandler<TestEvent>(logger)
{
    public override Task HandleAsync(TestEvent @event)
    {
        Assert.Equal(@event, TestEventData.Message);

        return Task.CompletedTask;
    }
}
