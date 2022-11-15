using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Queue.Internal;

internal sealed class QueueProcessorHost<T> : IHostedService where T : class
{
    private readonly ILogger<QueueProcessorHost<T>> logger;
    private readonly IQueue queue;
    private readonly IServiceProvider serviceProvider;
    private QueueSubscribeResult? subscriptionResult;

    public QueueProcessorHost(IQueue queue, IServiceProvider serviceProvider, ILogger<QueueProcessorHost<T>> logger)
    {
        this.queue = queue;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Start processing messages of type {Type}", typeof(T));
        var result = await queue.SubscribeAsync<T>(async (message, context) =>
        {
            logger.LogDebug("New message of {Type}", typeof(T));
            using var scope = serviceProvider.CreateScope();
            var processors = scope.ServiceProvider.GetServices<IQueueProcessor<T>>().ToArray();
            logger.LogDebug("Processors of type {Type}: {Count}", typeof(T), processors.Length);
            foreach (var processor in processors)
            {
                try
                {
                    logger.LogDebug("Run processor {Processor}", processor.GetType());
                    await processor.ProcessAsync(message, context);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error while processing message {MessageType}: {ErrorText}", typeof(T),
                        e.ToString());
                }
            }

            return true;
        });

        if (result.IsSuccess)
        {
            subscriptionResult = result;
        }
        else
        {
            throw new InvalidOperationException($"Can't subscribe to messages of type {typeof(T)}");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (subscriptionResult != null)
        {
            await queue.UnsubscribeAsync<T>(subscriptionResult.SubscriptionId);
        }
    }
}

