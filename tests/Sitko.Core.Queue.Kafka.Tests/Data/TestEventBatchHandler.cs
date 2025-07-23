using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.Kafka.Tests.Data;

[QueueBatchHandler("BatchTestHandlers", 10, 1)]
public class TestEventBatchHandler(ILogger<TestEventBatchHandler> logger) : BaseBatchMessageHandler<BatchTestEvent>
{
    protected override Task HandleAsync(BatchTestEvent[] messages, CancellationToken cancellationToken)
    {
        foreach (var message in messages)
        {
            logger.LogInformation("BatchTestEvent {Id} {Name}", message.Id, message.Name);
            EventRegistrator.Register(message.Id);
        }

        EventRegistrator.RegisterBatch(messages.Select(message => message.Id).ToArray());

        return Task.CompletedTask;
    }
}
