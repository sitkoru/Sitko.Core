using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.Kafka.Tests.Data;

[QueueHandler("TestHandlers")]
public class TestEventHandler(ILogger<TestEventHandler> logger) : BaseMessageHandler<TestEvent>
{
    protected override Task Handle(TestEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("TestEvent {Id} {Name}", message.Id, message.Name);
        EventRegistrator.Register(message.Id);
        return Task.CompletedTask;
    }
}
